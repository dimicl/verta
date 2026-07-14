using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class DeleteCommentCommand : ICommand<bool>
{
    private readonly int _commentId;
    private readonly int _userId;
    private readonly ICommentRepository _commentRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly DomainEventSubject _domainEventSubject;

    public DeleteCommentCommand(
        int commentId,
        int userId,
        ICommentRepository commentRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IWorkItemLockService workItemLockService,
        DomainEventSubject domainEventSubject)
    {
        _commentId = commentId;
        _userId = userId;
        _commentRepo = commentRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _workItemLockService = workItemLockService;
        _domainEventSubject = domainEventSubject;
    }

    public async Task<bool> ExecuteAsync()
    {
        var comment = await _commentRepo.GetByIdWithUserAsync(_commentId);
        if (comment == null)
            throw new NotFoundException("Comment does not exist.");

        if (comment.UserId != _userId)
            throw new ForbiddenException("You can only delete your own comments.");

        var workItem = await _workItemRepo.GetById(comment.WorkItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);
        await _workItemLockService.EnsureUserHasWriteLockAsync(comment.WorkItemId);

        await _commentRepo.Delete(comment);

        await _domainEventSubject.NotifyAsync(DomainEventNames.CommentDeleted, new
        {
            CommentId = comment.Id,
            WorkItemId = comment.WorkItemId,
            BoardId = workItem.BoardId,
            UserId = comment.UserId
        });

        return true;
    }
}
