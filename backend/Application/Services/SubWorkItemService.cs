using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class SubWorkItemService : ISubWorkItemService
{
    private readonly ISubWorkItemRepository _subRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;

    public SubWorkItemService(
        ISubWorkItemRepository subRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker)
    {
        _subRepo = subRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _commandInvoker = commandInvoker;
    }

    public async Task<SubWorkItemResponse> Create(SubWorkItemRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("Sub work item name is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new Exception("Sub work item description is required.");

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

        if (member.Role == UserRole.Guest)
            throw new Exception("Guest cannot create sub work item.");

        var subWorkItem = new SubWorkItem
        {
            Name = request.Name,
            Description = request.Description,
            WorkItemId = request.WorkItemId,
            UserId = userId,
            Status = WorkItemStatus.ToDo,
            CreatedAt = DateTime.UtcNow
        };

        var command = new CreateSubWorkItemCommand(async () =>
        {
            var created = await _subRepo.Add(subWorkItem);

            await _domainEventSubject.NotifyAsync("SubWorkItemCreated", new
            {
                SubWorkItemId = created.Id,
                WorkItemId = created.WorkItemId,
                UserId = created.UserId,
                Status = created.Status
            });

            return SubWorkItemHelper.ToResponse(created);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<List<SubWorkItemResponse>> GetByWorkItemId(int workItemId)
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

        var subItems = await _subRepo.GetByWorkItemIdAsync(workItemId);

        return subItems.Select(SubWorkItemHelper.ToResponse).ToList();
    }

    public async Task<SubWorkItemResponse> ChangeStatus(
        int subWorkItemId,
        ChangeSubWorkItemStatusRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        var userId = _userContext.GetUserId();

        var subWorkItem = await _subRepo.GetById(subWorkItemId);
        if (subWorkItem == null)
            throw new Exception("Sub work item does not exist.");

        var workItem = await _workItemRepo.GetById(subWorkItem.WorkItemId);
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

        if (member.Role == UserRole.Guest)
            throw new Exception("Guest cannot change sub work item status.");

        var currentState = WorkItemStateFactory.Create(subWorkItem.Status);

        if (!currentState.CanMoveTo(request.Status))
        {
            throw new Exception(
                $"Invalid status transition from {subWorkItem.Status} to {request.Status}."
            );
        }

        var command = new ChangeSubWorkItemStatusCommand(async () =>
        {
            subWorkItem.Status = request.Status;
            subWorkItem.UpdatedAt = DateTime.UtcNow;

            await _subRepo.Update(subWorkItem);

            await _domainEventSubject.NotifyAsync("SubWorkItemStatusChanged", new
            {
                SubWorkItemId = subWorkItem.Id,
                WorkItemId = subWorkItem.WorkItemId,
                Status = subWorkItem.Status,
                UpdatedByUserId = userId
            });

            return SubWorkItemHelper.ToResponse(subWorkItem);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }
}