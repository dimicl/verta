using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;

    public CommentService(
        ICommentRepository commentRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker)
    {
        _commentRepo = commentRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
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

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            board.WorkspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

        var comment = new Comment
        {
            Content = request.Content,
            WorkItemId = request.WorkItemId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var command = new AddCommentCommand(async () =>
        {
            var createdComment = await _commentRepo.Add(comment);

            await _domainEventSubject.NotifyAsync("CommentCreated", new
            {
                CommentId = createdComment.Id,
                WorkItemId = createdComment.WorkItemId,
                UserId = createdComment.UserId,
                Content = createdComment.Content
            });

            return CommentHelper.ToResponse(createdComment);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<List<CommentResponse>> GetByWorkItemId(int workItemId)
    {
        var userId = _userContext.GetUserId();

        var workItem = await _workItemRepo.GetById(workItemId);

        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);

        if (board == null)
            throw new Exception("Board does not exist.");

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            board.WorkspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

        var comments = await _commentRepo.GetByWorkItemIdAsync(workItemId);

        return comments
            .Select(CommentHelper.ToResponse)
            .ToList();
    }
}