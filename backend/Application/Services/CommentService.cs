using backend.Application.Interfaces;

using backend.Application.Exceptions;
namespace backend.Application.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly ISubWorkItemRepository _subWorkItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;

    public CommentService(
        ICommentRepository commentRepo,
        IWorkItemRepository workItemRepo,
        ISubWorkItemRepository subWorkItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IWorkItemLockService workItemLockService,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker)
    {
        _commentRepo = commentRepo;
        _workItemRepo = workItemRepo;
        _subWorkItemRepo = subWorkItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _workItemLockService = workItemLockService;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _commandInvoker = commandInvoker;
    }

    public async Task<CommentResponse> Create(CommentRequest request)
    {
        var command = new AddCommentCommand(
            request,
            _userContext.GetUserId(),
            _commentRepo,
            _workItemRepo,
            _subWorkItemRepo,
            _boardRepo,
            _boardAccessService,
            _workItemLockService,
            _domainEventSubject);

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<List<CommentResponse>> GetByWorkItemId(int workItemId)
    {
        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var comments = await _commentRepo.GetByWorkItemIdAsync(workItemId);

        return comments
            .Select(CommentHelper.ToResponse)
            .ToList();
    }

    public async Task<List<CommentResponse>> GetBySubWorkItemId(int subWorkItemId)
    {
        var subWorkItem = await _subWorkItemRepo.GetById(subWorkItemId);
        if (subWorkItem == null)
            throw new NotFoundException("Sub work item does not exist.");

        await EnsureWorkItemBoardAccessAsync(subWorkItem.WorkItemId);

        var comments = await _commentRepo.GetBySubWorkItemIdAsync(subWorkItemId);

        return comments
            .Select(CommentHelper.ToResponse)
            .ToList();
    }

    public async Task<CommentResponse> Update(int commentId, UpdateCommentRequest request)
    {
        var command = new UpdateCommentCommand(
            commentId,
            request,
            _userContext.GetUserId(),
            _commentRepo,
            _workItemRepo,
            _boardRepo,
            _boardAccessService,
            _workItemLockService,
            _domainEventSubject);

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task Delete(int commentId)
    {
        var command = new DeleteCommentCommand(
            commentId,
            _userContext.GetUserId(),
            _commentRepo,
            _workItemRepo,
            _boardRepo,
            _boardAccessService,
            _workItemLockService,
            _domainEventSubject);

        await _commandInvoker.ExecuteAsync(command);
    }

    private async Task EnsureWorkItemBoardAccessAsync(int workItemId)
    {
        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);
    }
}
