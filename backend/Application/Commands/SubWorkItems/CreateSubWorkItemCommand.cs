using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class CreateSubWorkItemCommand : ICommand<SubWorkItemResponse>
{
    private readonly SubWorkItemRequest _request;
    private readonly int _userId;
    private readonly ISubWorkItemRepository _subRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly DomainEventSubject _domainEventSubject;

    public CreateSubWorkItemCommand(
        SubWorkItemRequest request,
        int userId,
        ISubWorkItemRepository subRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IBoardAccessService boardAccessService,
        IWorkItemLockService workItemLockService,
        DomainEventSubject domainEventSubject)
    {
        _request = request;
        _userId = userId;
        _subRepo = subRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _boardAccessService = boardAccessService;
        _workItemLockService = workItemLockService;
        _domainEventSubject = domainEventSubject;
    }

    public async Task<SubWorkItemResponse> ExecuteAsync()
    {
        if (_request == null)
            throw new ValidationException("Request not found.");

        if (string.IsNullOrWhiteSpace(_request.Name))
            throw new ValidationException("Sub work item name is required.");

        if (string.IsNullOrWhiteSpace(_request.Description))
            throw new ValidationException("Sub work item description is required.");

        var workItem = await _workItemRepo.GetById(_request.WorkItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);
        await _workItemLockService.EnsureUserHasWriteLockAsync(_request.WorkItemId);
        await EnsureAssignedUserIsWorkspaceMemberAsync(board.WorkspaceId, _request.AssignedUserId);

        var subWorkItem = new SubWorkItem
        {
            Name = _request.Name,
            Description = _request.Description,
            WorkItemId = _request.WorkItemId,
            UserId = _userId,
            AssignedUserId = _request.AssignedUserId,
            Priority = _request.Priority,
            Status = WorkItemStatus.ToDo,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _subRepo.Add(subWorkItem);

        await _domainEventSubject.NotifyAsync(DomainEventNames.SubWorkItemCreated, new
        {
            SubWorkItemId = created.Id,
            WorkItemId = created.WorkItemId,
            BoardId = workItem.BoardId,
            UserId = created.UserId,
            Status = created.Status
        });

        return SubWorkItemHelper.ToResponse(created);
    }

    private async Task EnsureAssignedUserIsWorkspaceMemberAsync(int workspaceId, int? assignedUserId)
    {
        if (!assignedUserId.HasValue)
            return;

        var assignedMember = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            workspaceId,
            assignedUserId.Value
        );

        if (assignedMember == null)
            throw new ForbiddenException("Assigned user is not member of this workspace.");
    }
}
