using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class ChangeWorkItemAssigneeCommand : ICommand<WorkItemResponse>
{
    private readonly int _workItemId;
    private readonly ChangeWorkItemAssigneeRequest _request;
    private readonly int _userId;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly DomainEventSubject _domainEventSubject;

    public ChangeWorkItemAssigneeCommand(
        int workItemId,
        ChangeWorkItemAssigneeRequest request,
        int userId,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IBoardAccessService boardAccessService,
        DomainEventSubject domainEventSubject)
    {
        _workItemId = workItemId;
        _request = request;
        _userId = userId;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _boardAccessService = boardAccessService;
        _domainEventSubject = domainEventSubject;
    }

    public async Task<WorkItemResponse> ExecuteAsync()
    {
        if (_request == null)
            throw new ValidationException("Request not found.");

        var workItem = await _workItemRepo.GetById(_workItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

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

        workItem.AssignedUserId = _request.AssignedUserId;
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
