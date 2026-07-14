using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class CreateWorkItemCommand : ICommand<WorkItemResponse>
{
    private readonly WorkItemRequest _request;
    private readonly int _userId;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly ISprintRepository _sprintRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly DomainEventSubject _domainEventSubject;

    public CreateWorkItemCommand(
        WorkItemRequest request,
        int userId,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        ISprintRepository sprintRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IBoardAccessService boardAccessService,
        DomainEventSubject domainEventSubject)
    {
        _request = request;
        _userId = userId;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _sprintRepo = sprintRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _boardAccessService = boardAccessService;
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

        var board = await _boardRepo.GetById(_request.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        if (_request.SprintId.HasValue)
        {
            var sprint = await _sprintRepo.GetById(_request.SprintId.Value);
            if (sprint == null)
                throw new NotFoundException("Sprint does not exist.");

            if (sprint.BoardId != _request.BoardId)
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

        var workItem = new WorkItem
        {
            Name = _request.Name,
            Description = _request.Description,
            BoardId = _request.BoardId,
            SprintId = _request.SprintId,
            CreatedByUserId = _userId,
            AssignedUserId = _request.AssignedUserId,
            Priority = _request.Priority,
            Status = WorkItemStatus.ToDo,
            CreatedAt = DateTime.UtcNow
        };

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
                AssignedByUserId = _userId
            });
        }

        return WorkItemHelper.ToResponse(created);
    }
}
