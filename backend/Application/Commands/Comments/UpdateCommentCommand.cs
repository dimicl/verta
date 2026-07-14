using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class UpdateCommentCommand : ICommand<CommentResponse>
{
    private readonly int _commentId;
    private readonly UpdateCommentRequest _request;
    private readonly int _userId;
    private readonly ICommentRepository _commentRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly DomainEventSubject _domainEventSubject;

    public UpdateCommentCommand(
        int commentId,
        UpdateCommentRequest request,
        int userId,
        ICommentRepository commentRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IWorkItemLockService workItemLockService,
        DomainEventSubject domainEventSubject)
    {
        _commentId = commentId;
        _request = request;
        _userId = userId;
        _commentRepo = commentRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _workItemLockService = workItemLockService;
        _domainEventSubject = domainEventSubject;
    }

    public async Task<CommentResponse> ExecuteAsync()
    {
        if (_request == null)
            throw new ValidationException("Request not found.");

        if (string.IsNullOrWhiteSpace(_request.Content))
            throw new ValidationException("Comment content is required.");

        var comment = await _commentRepo.GetByIdWithUserAsync(_commentId);
        if (comment == null)
            throw new NotFoundException("Comment does not exist.");

        if (comment.UserId != _userId)
            throw new ForbiddenException("You can only edit your own comments.");

        var boardId = await EnsureWorkItemBoardAccessAsync(comment.WorkItemId);
        await _workItemLockService.EnsureUserHasWriteLockAsync(comment.WorkItemId);

        comment.Content = _request.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;

        await _commentRepo.Update(comment);

        await _domainEventSubject.NotifyAsync(DomainEventNames.CommentUpdated, new
        {
            CommentId = comment.Id,
            WorkItemId = comment.WorkItemId,
            BoardId = boardId,
            UserId = comment.UserId,
            Content = comment.Content
        });

        return CommentHelper.ToResponse(comment);
    }

    private async Task<int> EnsureWorkItemBoardAccessAsync(int workItemId)
    {
        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);
        return workItem.BoardId;
    }
}
