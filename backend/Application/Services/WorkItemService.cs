using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class WorkItemService : IWorkItemService
{
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;
    private readonly INotificationService _notificationService;

    public WorkItemService(
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker,
        INotificationService notificationService)
    {
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _commandInvoker = commandInvoker;
        _notificationService = notificationService;
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

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            board.WorkspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

        if (member.Role == UserRole.Guest)
            throw new Exception("Guest cannot create work item.");

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
            CreatedByUserId = userId,
            AssignedUserId = request.AssignedUserId,
            Priority = request.Priority,
            Status = WorkItemStatus.ToDo,
            CreatedAt = DateTime.UtcNow
        };

        var command = new CreateWorkItemCommand(async () =>
        {
            var created = await _workItemRepo.Add(workItem);

            await _domainEventSubject.NotifyAsync("WorkItemCreated", new
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
                await _notificationService.SendToUserAsync(
                    created.AssignedUserId.Value,
                    "WorkItemAssigned",
                    new
                    {
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

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            board.WorkspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

        var workItems = await _workItemRepo.GetByBoardIdAsync(boardId);

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

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            board.WorkspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

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

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            board.WorkspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

        if (member.Role == UserRole.Guest)
            throw new Exception("Guest cannot change work item status.");

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

            await _domainEventSubject.NotifyAsync("WorkItemStatusChanged", new
            {
                WorkItemId = workItem.Id,
                BoardId = workItem.BoardId,
                Status = workItem.Status,
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

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            board.WorkspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

        if (member.Role == UserRole.Guest)
            throw new Exception("Guest cannot update work item.");

        if (request.AssignedUserId.HasValue)
        {
            var assignedMember = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
                board.WorkspaceId,
                request.AssignedUserId.Value
            );

            if (assignedMember == null)
                throw new Exception("Assigned user is not member of this workspace.");
        }

        workItem.Name = request.Name;
        workItem.Description = request.Description;
        workItem.AssignedUserId = request.AssignedUserId;
        workItem.Priority = request.Priority;
        workItem.UpdatedAt = DateTime.UtcNow;

        await _workItemRepo.Update(workItem);

        await _domainEventSubject.NotifyAsync("WorkItemUpdated", new
        {
            WorkItemId = workItem.Id,
            BoardId = workItem.BoardId,
            UpdatedByUserId = userId
        });

        return WorkItemHelper.ToResponse(workItem);
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

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            board.WorkspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

        if (member.Role == UserRole.Guest)
            throw new Exception("Guest cannot delete work item.");

        await _workItemRepo.Delete(workItem);

        await _domainEventSubject.NotifyAsync("WorkItemDeleted", new
        {
            WorkItemId = workItem.Id,
            BoardId = workItem.BoardId,
            DeletedByUserId = userId
        });
    }
}