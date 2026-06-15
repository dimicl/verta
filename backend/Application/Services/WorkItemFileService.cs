using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class WorkItemFileService : IWorkItemFileService
{
    private readonly IWorkItemFileRepository _fileRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly SignalRDomainEventObserver _signalRObserver;
    private readonly RabbitMqDomainEventObserver _rabbitMqObserver;
    private readonly CommandInvoker _commandInvoker;
    

    public WorkItemFileService(
        IWorkItemFileRepository fileRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        SignalRDomainEventObserver signalRObserver,
        RabbitMqDomainEventObserver rabbitMqObserver,
        CommandInvoker commandInvoker)
    {
        _fileRepo = fileRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _signalRObserver = signalRObserver;
        _rabbitMqObserver = rabbitMqObserver;
        _commandInvoker = commandInvoker;

        _domainEventSubject.Attach(_signalRObserver);
        _domainEventSubject.Attach(_rabbitMqObserver);
    }

    public async Task<WorkItemFileResponse> Create(WorkItemFileRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new Exception("File name is required.");

        if (string.IsNullOrWhiteSpace(request.FileType))
            throw new Exception("File type is required.");

        if (string.IsNullOrWhiteSpace(request.FileUrl))
            throw new Exception("File URL is required.");

        if (request.FileSize <= 0)
            throw new Exception("File size must be greater than zero.");

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
            throw new Exception("Guest cannot add files.");

        var file = new WorkItemFile
        {
            WorkItemId = request.WorkItemId,
            FileName = request.FileName,
            FileType = request.FileType,
            FileSize = request.FileSize,
            FileUrl = request.FileUrl,
            FileThumbnailUrl = request.FileThumbnailUrl,
            CreatedAt = DateTime.UtcNow
        };

        var command = new AddWorkItemFileCommand(async () =>
        {
            var created = await _fileRepo.Add(file);

            await _domainEventSubject.NotifyAsync("WorkItemFileAdded", new
            {
                FileId = created.Id,
                WorkItemId = created.WorkItemId,
                FileName = created.FileName,
                FileType = created.FileType,
                FileSize = created.FileSize
            });

            return WorkItemFileHelper.ToResponse(created);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<List<WorkItemFileResponse>> GetByWorkItemId(int workItemId)
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

        var files = await _fileRepo.GetByWorkItemIdAsync(workItemId);

        return files.Select(WorkItemFileHelper.ToResponse).ToList();
    }

    public async Task Delete(int fileId)
    {
        var userId = _userContext.GetUserId();

        var file = await _fileRepo.GetById(fileId);
        if (file == null)
            throw new Exception("File does not exist.");

        var workItem = await _workItemRepo.GetById(file.WorkItemId);
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
            throw new Exception("Guest cannot delete files.");

        var command = new DeleteWorkItemFileCommand(async () =>
        {
            await _fileRepo.Delete(file);

            return true;
        });

        await _commandInvoker.ExecuteAsync(command);
    }
}