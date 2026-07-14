using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class UpdateWorkItemCommand : ICommand<WorkItemResponse>
{
    private readonly int _workItemId;
    private readonly WorkItemRequest _request;
    private readonly int _userId;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly ISprintRepository _sprintRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly DomainEventSubject _domainEventSubject;

    public UpdateWorkItemCommand(
        int workItemId,
        WorkItemRequest request,
        int userId,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        ISprintRepository sprintRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IBoardAccessService boardAccessService,
        IWorkItemLockService workItemLockService,
        DomainEventSubject domainEventSubject)
    {
        _workItemId = workItemId;
        _request = request;
        _userId = userId;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _sprintRepo = sprintRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _boardAccessService = boardAccessService;
        _workItemLockService = workItemLockService;
        _domainEventSubject = domainEventSubject;
    }

    public async Task<WorkItemResponse> ExecuteAsync()
    {
        if (_request == null)
            throw new ValidationException("Request not found.");

        if (string.IsNullOrWhiteSpace(_request.Name))
            throw new ValidationException("Work item name is required.");

        if (string.IsNullOrWhiteSpace(_request.Description))
            throw new ValidationException("Work item description is required.");

        var workItem = await _workItemRepo.GetById(_workItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        await _workItemLockService.EnsureUserHasWriteLockAsync(_workItemId);

        if (_request.SprintId.HasValue)
        {
            var sprint = await _sprintRepo.GetById(_request.SprintId.Value);
            if (sprint == null)
                throw new NotFoundException("Sprint does not exist.");

            if (sprint.BoardId != workItem.BoardId)
                throw new ValidationException("Sprint does not belong to this board.");
        }

        if (_request.AssignedUserId.HasValue)
        {
            var assignedMember = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
                board.WorkspaceId,
                _request.AssignedUserId.Value
            );

            if (assignedMember == null)
                throw new ForbiddenException("Assigned user is not member of this workspace.");
        }

        var previousAssignee = workItem.AssignedUserId;

        workItem.Name = _request.Name;
        workItem.Description = _request.Description;
        workItem.AssignedUserId = _request.AssignedUserId;
        workItem.Priority = _request.Priority;
        workItem.SprintId = _request.SprintId;
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
                AssignedByUserId = _userId
            });
        }

        await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemUpdated, new
        {
            WorkItemId = workItem.Id,
            BoardId = workItem.BoardId,
            UpdatedByUserId = _userId,
            Name = workItem.Name,
            Description = workItem.Description,
            Status = workItem.Status.ToString(),
            Priority = workItem.Priority,
            AssignedUserId = workItem.AssignedUserId,
            SprintId = workItem.SprintId
        });

        return WorkItemHelper.ToResponse(workItem);
    }
}
