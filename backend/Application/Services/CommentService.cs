using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;

    public CommentService(
        ICommentRepository commentRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker)
    {
        _commentRepo = commentRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _commandInvoker = commandInvoker;
    }

    public async Task<CommentResponse> Create(CommentRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        if (string.IsNullOrWhiteSpace(request.Content))
            throw new Exception("Comment content is required.");

        var userId = _userContext.GetUserId();
        var workItem = await _workItemRepo.GetById(request.WorkItemId);

        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);

        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var comment = new Comment
        {
            Content = request.Content.Trim(),
            WorkItemId = request.WorkItemId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var command = new AddCommentCommand(async () =>
        {
            var createdComment = await _commentRepo.Add(comment);
            var withUser = await _commentRepo.GetByIdWithUserAsync(createdComment.Id);

            await _domainEventSubject.NotifyAsync(DomainEventNames.CommentCreated, new
            {
                CommentId = createdComment.Id,
                WorkItemId = createdComment.WorkItemId,
                UserId = createdComment.UserId,
                Content = createdComment.Content
            });

            return CommentHelper.ToResponse(withUser ?? createdComment);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<List<CommentResponse>> GetByWorkItemId(int workItemId)
    {
        var workItem = await _workItemRepo.GetById(workItemId);

        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);

        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var comments = await _commentRepo.GetByWorkItemIdAsync(workItemId);

        return comments
            .Select(CommentHelper.ToResponse)
            .ToList();
    }

    public async Task<CommentResponse> Update(int commentId, UpdateCommentRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        if (string.IsNullOrWhiteSpace(request.Content))
            throw new Exception("Comment content is required.");

        var userId = _userContext.GetUserId();
        var comment = await _commentRepo.GetByIdWithUserAsync(commentId);

        if (comment == null)
            throw new Exception("Comment does not exist.");

        if (comment.UserId != userId)
            throw new Exception("You can only edit your own comments.");

        await EnsureWorkItemBoardAccessAsync(comment.WorkItemId);

        comment.Content = request.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;

        var command = new UpdateCommentCommand(async () =>
        {
            await _commentRepo.Update(comment);

            await _domainEventSubject.NotifyAsync(DomainEventNames.CommentUpdated, new
            {
                CommentId = comment.Id,
                WorkItemId = comment.WorkItemId,
                UserId = comment.UserId,
                Content = comment.Content
            });

            return CommentHelper.ToResponse(comment);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task Delete(int commentId)
    {
        var userId = _userContext.GetUserId();
        var comment = await _commentRepo.GetByIdWithUserAsync(commentId);

        if (comment == null)
            throw new Exception("Comment does not exist.");

        if (comment.UserId != userId)
            throw new Exception("You can only delete your own comments.");

        await EnsureWorkItemBoardAccessAsync(comment.WorkItemId);

        var command = new DeleteCommentCommand(async () =>
        {
            await _commentRepo.Delete(comment);

            await _domainEventSubject.NotifyAsync(DomainEventNames.CommentDeleted, new
            {
                CommentId = comment.Id,
                WorkItemId = comment.WorkItemId,
                UserId = comment.UserId
            });

            return true;
        });

        await _commandInvoker.ExecuteAsync(command);
    }

    private async Task EnsureWorkItemBoardAccessAsync(int workItemId)
    {
        var workItem = await _workItemRepo.GetById(workItemId);

        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);

        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);
    }
}
