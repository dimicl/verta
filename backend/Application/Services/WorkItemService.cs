using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class WorkItemService : IWorkItemService
{
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly ISprintRepository _sprintRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;
    private readonly IWorkItemLockService _workItemLockService;

    public WorkItemService(
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        ISprintRepository sprintRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IBoardAccessService boardAccessService,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker,
        IWorkItemLockService workItemLockService)
    {
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _sprintRepo = sprintRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _boardAccessService = boardAccessService;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _commandInvoker = commandInvoker;
        _workItemLockService = workItemLockService;
    }

    public async Task<WorkItemResponse> Create(WorkItemRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("Work item name is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new Exception("Work item description is required.");

        var userId = _userContext.GetUserId();

        var board = await _boardRepo.GetById(request.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        if (request.SprintId.HasValue)
        {
            var sprint = await _sprintRepo.GetById(request.SprintId.Value);
            if (sprint == null)
                throw new Exception("Sprint does not exist.");

            if (sprint.BoardId != request.BoardId)
                throw new Exception("Sprint does not belong to this board.");
        }

        if (request.AssignedUserId.HasValue)
        {
            var assignedMember = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
                board.WorkspaceId,
                request.AssignedUserId.Value
            );

            if (assignedMember == null)
                throw new Exception("Assigned user is not member of this workspace.");
        }

        var workItem = new WorkItem
        {
            Name = request.Name,
            Description = request.Description,
            BoardId = request.BoardId,
            SprintId = request.SprintId,
            CreatedByUserId = userId,
            AssignedUserId = request.AssignedUserId,
            Priority = request.Priority,
            Status = WorkItemStatus.ToDo,
            CreatedAt = DateTime.UtcNow
        };

        var command = new CreateWorkItemCommand(async () =>
        {
            var created = await _workItemRepo.Add(workItem);

            await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemCreated, new
            {
                WorkItemId = created.Id,
                BoardId = created.BoardId,
                CreatedByUserId = created.CreatedByUserId,
                AssignedUserId = created.AssignedUserId,
                Priority = created.Priority,
                Status = created.Status
            });

            if (created.AssignedUserId.HasValue)
            {
                await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemAssigned, new
                {
                    TargetUserId = created.AssignedUserId.Value,
                    WorkItemId = created.Id,
                    Name = created.Name,
                    BoardId = created.BoardId,
                    AssignedByUserId = userId
                });
            }

            return WorkItemHelper.ToResponse(created);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<List<WorkItemResponse>> GetByBoardId(int boardId)
    {
        var userId = _userContext.GetUserId();

        var board = await _boardRepo.GetById(boardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var workItems = await _workItemRepo.GetByBoardIdAsync(boardId);

        return workItems.Select(WorkItemHelper.ToResponse).ToList();
    }

    public async Task<List<WorkItemResponse>> GetBySprintId(int sprintId)
    {
        var userId = _userContext.GetUserId();

        var sprint = await _sprintRepo.GetById(sprintId);
        if (sprint == null)
            throw new Exception("Sprint does not exist.");

        var board = await _boardRepo.GetById(sprint.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var workItems = await _workItemRepo.GetBySprintIdAsync(sprintId);

        return workItems.Select(WorkItemHelper.ToResponse).ToList();
    }

    public async Task<WorkItemResponse> GetById(int workItemId)
    {
        var userId = _userContext.GetUserId();

        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        return WorkItemHelper.ToResponse(workItem);
    }

    public async Task<WorkItemResponse> ChangeStatus(int workItemId, ChangeWorkItemStatusRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        var userId = _userContext.GetUserId();

        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var currentState = WorkItemStateFactory.Create(workItem.Status);

        if (!currentState.CanMoveTo(request.Status))
        {
            throw new Exception(
                $"Invalid status transition from {workItem.Status} to {request.Status}."
            );
        }

        var command = new ChangeWorkItemStatusCommand(async () =>
        {
            workItem.Status = request.Status;
            workItem.UpdatedAt = DateTime.UtcNow;

            await _workItemRepo.Update(workItem);

            await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemStatusChanged, new
            {
                WorkItemId = workItem.Id,
                BoardId = workItem.BoardId,
                Name = workItem.Name,
                Status = workItem.Status,
                UpdatedByUserId = userId,
                TargetUserId = workItem.AssignedUserId
            });

            return WorkItemHelper.ToResponse(workItem);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<WorkItemResponse> ChangePriority(int workItemId, ChangeWorkItemPriorityRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        var userId = _userContext.GetUserId();

        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var command = new ChangeWorkItemPriorityCommand(async () =>
        {
            workItem.Priority = request.Priority;
            workItem.UpdatedAt = DateTime.UtcNow;

            await _workItemRepo.Update(workItem);

            await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemPriorityChanged, new
            {
                WorkItemId = workItem.Id,
                BoardId = workItem.BoardId,
                Priority = workItem.Priority,
                UpdatedByUserId = userId
            });

            return WorkItemHelper.ToResponse(workItem);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<WorkItemResponse> ChangeAssignee(
        int workItemId,
        ChangeWorkItemAssigneeRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        var userId = _userContext.GetUserId();

        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        if (request.AssignedUserId.HasValue)
        {
            var assignedMember = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
                board.WorkspaceId,
                request.AssignedUserId.Value
            );

            if (assignedMember == null)
                throw new Exception("Assigned user is not member of this workspace.");
        }

        var previousAssignee = workItem.AssignedUserId;

        var command = new ChangeWorkItemAssigneeCommand(async () =>
        {
            workItem.AssignedUserId = request.AssignedUserId;
            workItem.UpdatedAt = DateTime.UtcNow;

            await _workItemRepo.Update(workItem);

            if (workItem.AssignedUserId.HasValue && workItem.AssignedUserId != previousAssignee)
            {
                await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemAssigned, new
                {
                    TargetUserId = workItem.AssignedUserId.Value,
                    WorkItemId = workItem.Id,
                    Name = workItem.Name,
                    BoardId = workItem.BoardId,
                    AssignedByUserId = userId
                });
            }

            await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemUpdated, new
            {
                WorkItemId = workItem.Id,
                BoardId = workItem.BoardId,
                UpdatedByUserId = userId
            });

            return WorkItemHelper.ToResponse(workItem);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<WorkItemResponse> Update(int workItemId, WorkItemRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("Work item name is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new Exception("Work item description is required.");

        var userId = _userContext.GetUserId();

        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        await _workItemLockService.EnsureUserHasWriteLockAsync(workItemId);

        if (request.SprintId.HasValue)
        {
            var sprint = await _sprintRepo.GetById(request.SprintId.Value);
            if (sprint == null)
                throw new Exception("Sprint does not exist.");

            if (sprint.BoardId != workItem.BoardId)
                throw new Exception("Sprint does not belong to this board.");
        }

        if (request.AssignedUserId.HasValue)
        {
            var assignedMember = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
                board.WorkspaceId,
                request.AssignedUserId.Value
            );

            if (assignedMember == null)
                throw new Exception("Assigned user is not member of this workspace.");
        }

        var previousAssignee = workItem.AssignedUserId;

        var command = new UpdateWorkItemCommand(async () =>
        {
            workItem.Name = request.Name;
            workItem.Description = request.Description;
            workItem.AssignedUserId = request.AssignedUserId;
            workItem.Priority = request.Priority;
            workItem.SprintId = request.SprintId;
            workItem.UpdatedAt = DateTime.UtcNow;

            await _workItemRepo.Update(workItem);

            if (workItem.AssignedUserId.HasValue && workItem.AssignedUserId != previousAssignee)
            {
                await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemAssigned, new
                {
                    TargetUserId = workItem.AssignedUserId.Value,
                    WorkItemId = workItem.Id,
                    Name = workItem.Name,
                    BoardId = workItem.BoardId,
                    AssignedByUserId = userId
                });
            }

            await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemUpdated, new
            {
                WorkItemId = workItem.Id,
                BoardId = workItem.BoardId,
                UpdatedByUserId = userId
            });

            return WorkItemHelper.ToResponse(workItem);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task Delete(int workItemId)
    {
        var userId = _userContext.GetUserId();

        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        await _workItemLockService.EnsureUserHasWriteLockAsync(workItemId);

        var command = new DeleteWorkItemCommand(async () =>
        {
            await _workItemRepo.Delete(workItem);

            await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemDeleted, new
            {
                WorkItemId = workItem.Id,
                BoardId = workItem.BoardId,
                DeletedByUserId = userId
            });

            return true;
        });

        await _commandInvoker.ExecuteAsync(command);
    }
}