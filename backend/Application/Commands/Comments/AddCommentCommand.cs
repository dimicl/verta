using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class AddCommentCommand : ICommand<CommentResponse>
{
    private readonly CommentRequest _request;
    private readonly int _userId;
    private readonly ICommentRepository _commentRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly ISubWorkItemRepository _subWorkItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly DomainEventSubject _domainEventSubject;

    public AddCommentCommand(
        CommentRequest request,
        int userId,
        ICommentRepository commentRepo,
        IWorkItemRepository workItemRepo,
        ISubWorkItemRepository subWorkItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IWorkItemLockService workItemLockService,
        DomainEventSubject domainEventSubject)
    {
        _request = request;
        _userId = userId;
        _commentRepo = commentRepo;
        _workItemRepo = workItemRepo;
        _subWorkItemRepo = subWorkItemRepo;
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

        var workItem = await _workItemRepo.GetById(_request.WorkItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);
        await _workItemLockService.EnsureUserHasWriteLockAsync(_request.WorkItemId);

        if (_request.SubWorkItemId.HasValue)
        {
            var subWorkItem = await _subWorkItemRepo.GetById(_request.SubWorkItemId.Value);
            if (subWorkItem == null)
                throw new NotFoundException("Sub work item does not exist.");

            if (subWorkItem.WorkItemId != _request.WorkItemId)
                throw new ValidationException("Sub work item does not belong to this work item.");
        }

        var comment = new Comment
        {
            Content = _request.Content.Trim(),
            WorkItemId = _request.WorkItemId,
            SubWorkItemId = _request.SubWorkItemId,
            UserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        var createdComment = await _commentRepo.Add(comment);
        var withUser = await _commentRepo.GetByIdWithUserAsync(createdComment.Id);

        await _domainEventSubject.NotifyAsync(DomainEventNames.CommentCreated, new
        {
            CommentId = createdComment.Id,
            WorkItemId = createdComment.WorkItemId,
            BoardId = workItem.BoardId,
            UserId = createdComment.UserId,
            Content = createdComment.Content
        });

        return CommentHelper.ToResponse(withUser ?? createdComment);
    }
}
