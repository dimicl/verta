using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class ChangeWorkItemStatusCommand : ICommand<WorkItemResponse>
{
    private readonly int _workItemId;
    private readonly ChangeWorkItemStatusRequest _request;
    private readonly int _userId;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly DomainEventSubject _domainEventSubject;

    public ChangeWorkItemStatusCommand(
        int workItemId,
        ChangeWorkItemStatusRequest request,
        int userId,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        DomainEventSubject domainEventSubject)
    {
        _workItemId = workItemId;
        _request = request;
        _userId = userId;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
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

        var currentState = WorkItemStateFactory.Create(workItem.Status);

        if (!currentState.CanMoveTo(_request.Status))
        {
            throw new ValidationException($"Invalid status transition from {workItem.Status} to {_request.Status}.");
        }

        workItem.Status = _request.Status;
        workItem.UpdatedAt = DateTime.UtcNow;

        await _workItemRepo.Update(workItem);

        await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemStatusChanged, new
        {
            WorkItemId = workItem.Id,
            BoardId = workItem.BoardId,
            Name = workItem.Name,
            Status = workItem.Status,
            UpdatedByUserId = _userId,
            TargetUserId = workItem.AssignedUserId
        });

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
