using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class ChangeSubWorkItemStatusCommand : ICommand<SubWorkItemResponse>
{
    private readonly int _subWorkItemId;
    private readonly ChangeSubWorkItemStatusRequest _request;
    private readonly int _userId;
    private readonly ISubWorkItemRepository _subRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly DomainEventSubject _domainEventSubject;

    public ChangeSubWorkItemStatusCommand(
        int subWorkItemId,
        ChangeSubWorkItemStatusRequest request,
        int userId,
        ISubWorkItemRepository subRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
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
        _boardAccessService = boardAccessService;
        _workItemLockService = workItemLockService;
        _domainEventSubject = domainEventSubject;
    }

    public async Task<SubWorkItemResponse> ExecuteAsync()
    {
        if (_request == null)
            throw new ValidationException("Request not found.");

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

        var currentState = WorkItemStateFactory.Create(subWorkItem.Status);

        if (!currentState.CanMoveTo(_request.Status))
        {
            throw new ValidationException($"Invalid status transition from {subWorkItem.Status} to {_request.Status}.");
        }

        subWorkItem.Status = _request.Status;
        subWorkItem.UpdatedAt = DateTime.UtcNow;

        await _subRepo.Update(subWorkItem);

        await _domainEventSubject.NotifyAsync(DomainEventNames.SubWorkItemStatusChanged, new
        {
            SubWorkItemId = subWorkItem.Id,
            WorkItemId = subWorkItem.WorkItemId,
            BoardId = workItem.BoardId,
            Status = subWorkItem.Status,
            UpdatedByUserId = _userId
        });

        return SubWorkItemHelper.ToResponse(subWorkItem);
    }
}
