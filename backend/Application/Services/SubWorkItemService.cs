using backend.Application.Interfaces;

using backend.Application.Exceptions;
namespace backend.Application.Services;

public class SubWorkItemService : ISubWorkItemService
{
    private readonly ISubWorkItemRepository _subRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;

    public SubWorkItemService(
        ISubWorkItemRepository subRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IBoardAccessService boardAccessService,
        IWorkItemLockService workItemLockService,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker)
    {
        _subRepo = subRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _boardAccessService = boardAccessService;
        _workItemLockService = workItemLockService;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _commandInvoker = commandInvoker;
    }

    public async Task<SubWorkItemResponse> Create(SubWorkItemRequest request)
    {
        var command = new CreateSubWorkItemCommand(
            request,
            _userContext.GetUserId(),
            _subRepo,
            _workItemRepo,
            _boardRepo,
            _workspaceMemberRepo,
            _boardAccessService,
            _workItemLockService,
            _domainEventSubject);

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<SubWorkItemResponse> GetById(int subWorkItemId)
    {
        var subWorkItem = await _subRepo.GetById(subWorkItemId);
        if (subWorkItem == null)
            throw new NotFoundException("Sub work item does not exist.");

        var workItem = await _workItemRepo.GetById(subWorkItem.WorkItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        return SubWorkItemHelper.ToResponse(subWorkItem);
    }

    public async Task<List<SubWorkItemResponse>> GetByWorkItemId(int workItemId)
    {
        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var subItems = await _subRepo.GetByWorkItemIdAsync(workItemId);

        return subItems.Select(SubWorkItemHelper.ToResponse).ToList();
    }

    public async Task<SubWorkItemResponse> Update(
        int subWorkItemId,
        UpdateSubWorkItemRequest request)
    {
        var command = new UpdateSubWorkItemCommand(
            subWorkItemId,
            request,
            _userContext.GetUserId(),
            _subRepo,
            _workItemRepo,
            _boardRepo,
            _workspaceMemberRepo,
            _boardAccessService,
            _workItemLockService,
            _domainEventSubject);

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<SubWorkItemResponse> ChangeStatus(
        int subWorkItemId,
        ChangeSubWorkItemStatusRequest request)
    {
        var command = new ChangeSubWorkItemStatusCommand(
            subWorkItemId,
            request,
            _userContext.GetUserId(),
            _subRepo,
            _workItemRepo,
            _boardRepo,
            _boardAccessService,
            _workItemLockService,
            _domainEventSubject);

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task Delete(int subWorkItemId)
    {
        var command = new DeleteSubWorkItemCommand(
            subWorkItemId,
            _userContext.GetUserId(),
            _subRepo,
            _workItemRepo,
            _boardRepo,
            _boardAccessService,
            _workItemLockService,
            _domainEventSubject);

        await _commandInvoker.ExecuteAsync(command);
    }
}
