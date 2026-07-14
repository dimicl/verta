using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class DeleteSubWorkItemCommand : ICommand<bool>
{
    private readonly int _subWorkItemId;
    private readonly int _userId;
    private readonly ISubWorkItemRepository _subRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly DomainEventSubject _domainEventSubject;

    public DeleteSubWorkItemCommand(
        int subWorkItemId,
        int userId,
        ISubWorkItemRepository subRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IWorkItemLockService workItemLockService,
        DomainEventSubject domainEventSubject)
    {
        _subWorkItemId = subWorkItemId;
        _userId = userId;
        _subRepo = subRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _workItemLockService = workItemLockService;
        _domainEventSubject = domainEventSubject;
    }

    public async Task<bool> ExecuteAsync()
    {
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

        await _subRepo.Delete(subWorkItem);

        await _domainEventSubject.NotifyAsync(DomainEventNames.SubWorkItemDeleted, new
        {
            SubWorkItemId = subWorkItem.Id,
            WorkItemId = subWorkItem.WorkItemId,
            BoardId = workItem.BoardId,
            DeletedByUserId = _userId
        });

        return true;
    }
}
