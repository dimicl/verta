using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class SubWorkItemService : ISubWorkItemService
{
    private readonly ISubWorkItemRepository _subRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;

    public SubWorkItemService(
        ISubWorkItemRepository subRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IBoardAccessService boardAccessService,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker)
    {
        _subRepo = subRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _boardAccessService = boardAccessService;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _commandInvoker = commandInvoker;
    }

    public async Task<SubWorkItemResponse> Create(SubWorkItemRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("Sub work item name is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new Exception("Sub work item description is required.");

        var userId = _userContext.GetUserId();

        var workItem = await _workItemRepo.GetById(request.WorkItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);
        await EnsureAssignedUserIsWorkspaceMemberAsync(board.WorkspaceId, request.AssignedUserId);

        var subWorkItem = new SubWorkItem
        {
            Name = request.Name,
            Description = request.Description,
            WorkItemId = request.WorkItemId,
            UserId = userId,
            AssignedUserId = request.AssignedUserId,
            Priority = request.Priority,
            Status = WorkItemStatus.ToDo,
            CreatedAt = DateTime.UtcNow
        };

        var command = new CreateSubWorkItemCommand(async () =>
        {
            var created = await _subRepo.Add(subWorkItem);

            await _domainEventSubject.NotifyAsync(DomainEventNames.SubWorkItemCreated, new
            {
                SubWorkItemId = created.Id,
                WorkItemId = created.WorkItemId,
                UserId = created.UserId,
                Status = created.Status
            });

            return SubWorkItemHelper.ToResponse(created);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<SubWorkItemResponse> GetById(int subWorkItemId)
    {
        var subWorkItem = await _subRepo.GetById(subWorkItemId);
        if (subWorkItem == null)
            throw new Exception("Sub work item does not exist.");

        var workItem = await _workItemRepo.GetById(subWorkItem.WorkItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        return SubWorkItemHelper.ToResponse(subWorkItem);
    }

    public async Task<List<SubWorkItemResponse>> GetByWorkItemId(int workItemId)
    {
        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var subItems = await _subRepo.GetByWorkItemIdAsync(workItemId);

        return subItems.Select(SubWorkItemHelper.ToResponse).ToList();
    }

    public async Task<SubWorkItemResponse> Update(
        int subWorkItemId,
        UpdateSubWorkItemRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("Sub work item name is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new Exception("Sub work item description is required.");

        var userId = _userContext.GetUserId();

        var subWorkItem = await _subRepo.GetById(subWorkItemId);
        if (subWorkItem == null)
            throw new Exception("Sub work item does not exist.");

        var workItem = await _workItemRepo.GetById(subWorkItem.WorkItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);
        await EnsureAssignedUserIsWorkspaceMemberAsync(board.WorkspaceId, request.AssignedUserId);

        var command = new UpdateSubWorkItemCommand(async () =>
        {
            subWorkItem.Name = request.Name;
            subWorkItem.Description = request.Description;
            subWorkItem.AssignedUserId = request.AssignedUserId;
            subWorkItem.Priority = request.Priority;
            subWorkItem.UpdatedAt = DateTime.UtcNow;

            await _subRepo.Update(subWorkItem);

            await _domainEventSubject.NotifyAsync(DomainEventNames.SubWorkItemUpdated, new
            {
                SubWorkItemId = subWorkItem.Id,
                WorkItemId = subWorkItem.WorkItemId,
                UpdatedByUserId = userId
            });

            return SubWorkItemHelper.ToResponse(subWorkItem);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<SubWorkItemResponse> ChangeStatus(
        int subWorkItemId,
        ChangeSubWorkItemStatusRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        var userId = _userContext.GetUserId();

        var subWorkItem = await _subRepo.GetById(subWorkItemId);
        if (subWorkItem == null)
            throw new Exception("Sub work item does not exist.");

        var workItem = await _workItemRepo.GetById(subWorkItem.WorkItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var currentState = WorkItemStateFactory.Create(subWorkItem.Status);

        if (!currentState.CanMoveTo(request.Status))
        {
            throw new Exception(
                $"Invalid status transition from {subWorkItem.Status} to {request.Status}."
            );
        }

        var command = new ChangeSubWorkItemStatusCommand(async () =>
        {
            subWorkItem.Status = request.Status;
            subWorkItem.UpdatedAt = DateTime.UtcNow;

            await _subRepo.Update(subWorkItem);

            await _domainEventSubject.NotifyAsync(DomainEventNames.SubWorkItemStatusChanged, new
            {
                SubWorkItemId = subWorkItem.Id,
                WorkItemId = subWorkItem.WorkItemId,
                Status = subWorkItem.Status,
                UpdatedByUserId = userId
            });

            return SubWorkItemHelper.ToResponse(subWorkItem);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task Delete(int subWorkItemId)
    {
        var userId = _userContext.GetUserId();

        var subWorkItem = await _subRepo.GetById(subWorkItemId);
        if (subWorkItem == null)
            throw new Exception("Sub work item does not exist.");

        var workItem = await _workItemRepo.GetById(subWorkItem.WorkItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var command = new DeleteSubWorkItemCommand(async () =>
        {
            await _subRepo.Delete(subWorkItem);

            await _domainEventSubject.NotifyAsync(DomainEventNames.SubWorkItemDeleted, new
            {
                SubWorkItemId = subWorkItem.Id,
                WorkItemId = subWorkItem.WorkItemId,
                DeletedByUserId = userId
            });

            return true;
        });

        await _commandInvoker.ExecuteAsync(command);
    }

    private async Task EnsureAssignedUserIsWorkspaceMemberAsync(
        int workspaceId,
        int? assignedUserId)
    {
        if (!assignedUserId.HasValue)
        {
            return;
        }

        var assignedMember = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            workspaceId,
            assignedUserId.Value
        );

        if (assignedMember == null)
            throw new Exception("Assigned user is not member of this workspace.");
    }
}
