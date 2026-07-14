using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class DeleteWorkItemCommand : ICommand<bool>
{
    private readonly int _workItemId;
    private readonly int _userId;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly DomainEventSubject _domainEventSubject;

    public DeleteWorkItemCommand(
        int workItemId,
        int userId,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IWorkItemLockService workItemLockService,
        DomainEventSubject domainEventSubject)
    {
        _workItemId = workItemId;
        _userId = userId;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _workItemLockService = workItemLockService;
        _domainEventSubject = domainEventSubject;
    }

    public async Task<bool> ExecuteAsync()
    {
        var workItem = await _workItemRepo.GetById(_workItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        await _workItemLockService.EnsureUserHasWriteLockAsync(_workItemId);

        await _workItemRepo.Delete(workItem);

        await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemDeleted, new
        {
            WorkItemId = workItem.Id,
            BoardId = workItem.BoardId,
            DeletedByUserId = _userId
        });

        return true;
    }
}
