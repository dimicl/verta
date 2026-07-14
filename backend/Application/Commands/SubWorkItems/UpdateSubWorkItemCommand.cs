using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class UpdateSubWorkItemCommand : ICommand<SubWorkItemResponse>
{
    private readonly int _subWorkItemId;
    private readonly UpdateSubWorkItemRequest _request;
    private readonly int _userId;
    private readonly ISubWorkItemRepository _subRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly DomainEventSubject _domainEventSubject;

    public UpdateSubWorkItemCommand(
        int subWorkItemId,
        UpdateSubWorkItemRequest request,
        int userId,
        ISubWorkItemRepository subRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IBoardAccessService boardAccessService,
        IWorkItemLockService workItemLockService,
        DomainEventSubject domainEventSubject)
    {
        _subWorkItemId = subWorkItemId;
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

        var subWorkItem = await _subRepo.GetById(_subWorkItemId);
        if (subWorkItem == null)
            throw new NotFoundException("Sub work item does not exist.");

        var workItem = await _workItemRepo.GetById(subWorkItem.WorkItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        await _workItemLockService.EnsureUserHasWriteLockAsync(workItem.Id);

        if (_request.AssignedUserId.HasValue)
        {
            var assignedMember = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
                board.WorkspaceId,
                _request.AssignedUserId.Value
            );

            if (assignedMember == null)
                throw new ForbiddenException("Assigned user is not member of this workspace.");
        }

        subWorkItem.Name = _request.Name;
        subWorkItem.Description = _request.Description;
        subWorkItem.AssignedUserId = _request.AssignedUserId;
        subWorkItem.Priority = _request.Priority;
        subWorkItem.UpdatedAt = DateTime.UtcNow;

        await _subRepo.Update(subWorkItem);

        await _domainEventSubject.NotifyAsync(DomainEventNames.SubWorkItemUpdated, new
        {
            SubWorkItemId = subWorkItem.Id,
            WorkItemId = subWorkItem.WorkItemId,
            BoardId = workItem.BoardId,
            UpdatedByUserId = _userId
        });

        return SubWorkItemHelper.ToResponse(subWorkItem);
    }
}
