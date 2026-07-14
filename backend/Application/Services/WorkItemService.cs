using backend.Application.Interfaces;

using backend.Application.Exceptions;
namespace backend.Application.Services;

public class WorkItemService : IWorkItemService
{
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly ISprintRepository _sprintRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;
    private readonly IWorkItemLockService _workItemLockService;

    public WorkItemService(
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        ISprintRepository sprintRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IBoardAccessService boardAccessService,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker,
        IWorkItemLockService workItemLockService)
    {
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _sprintRepo = sprintRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _boardAccessService = boardAccessService;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _commandInvoker = commandInvoker;
        _workItemLockService = workItemLockService;
    }

    public async Task<WorkItemResponse> Create(WorkItemRequest request)
    {
        var command = new CreateWorkItemCommand(
            request,
            _userContext.GetUserId(),
            _workItemRepo,
            _boardRepo,
            _sprintRepo,
            _workspaceMemberRepo,
            _boardAccessService,
            _domainEventSubject);

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<List<WorkItemResponse>> GetByBoardId(int boardId)
    {
        var board = await _boardRepo.GetById(boardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var workItems = await _workItemRepo.GetByBoardIdAsync(boardId);

        return workItems.Select(WorkItemHelper.ToResponse).ToList();
    }

    public async Task<List<WorkItemResponse>> GetBySprintId(int sprintId)
    {
        var sprint = await _sprintRepo.GetById(sprintId);
        if (sprint == null)
            throw new NotFoundException("Sprint does not exist.");

        var board = await _boardRepo.GetById(sprint.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var workItems = await _workItemRepo.GetBySprintIdAsync(sprintId);

        return workItems.Select(WorkItemHelper.ToResponse).ToList();
    }

    public async Task<WorkItemResponse> GetById(int workItemId)
    {
        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        return WorkItemHelper.ToResponse(workItem);
    }

    public async Task<WorkItemResponse> ChangeStatus(int workItemId, ChangeWorkItemStatusRequest request)
    {
        var command = new ChangeWorkItemStatusCommand(
            workItemId,
            request,
            _userContext.GetUserId(),
            _workItemRepo,
            _boardRepo,
            _boardAccessService,
            _domainEventSubject);

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<WorkItemResponse> ChangePriority(int workItemId, ChangeWorkItemPriorityRequest request)
    {
        var command = new ChangeWorkItemPriorityCommand(
            workItemId,
            request,
            _userContext.GetUserId(),
            _workItemRepo,
            _boardRepo,
            _boardAccessService,
            _domainEventSubject);

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<WorkItemResponse> ChangeAssignee(
        int workItemId,
        ChangeWorkItemAssigneeRequest request)
    {
        var command = new ChangeWorkItemAssigneeCommand(
            workItemId,
            request,
            _userContext.GetUserId(),
            _workItemRepo,
            _boardRepo,
            _workspaceMemberRepo,
            _boardAccessService,
            _domainEventSubject);

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<WorkItemResponse> Update(int workItemId, WorkItemRequest request)
    {
        var command = new UpdateWorkItemCommand(
            workItemId,
            request,
            _userContext.GetUserId(),
            _workItemRepo,
            _boardRepo,
            _sprintRepo,
            _workspaceMemberRepo,
            _boardAccessService,
            _workItemLockService,
            _domainEventSubject);

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task Delete(int workItemId)
    {
        var command = new DeleteWorkItemCommand(
            workItemId,
            _userContext.GetUserId(),
            _workItemRepo,
            _boardRepo,
            _boardAccessService,
            _workItemLockService,
            _domainEventSubject);

        await _commandInvoker.ExecuteAsync(command);
    }
}
