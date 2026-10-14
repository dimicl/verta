using backend.Application.Interfaces;
using Microsoft.AspNetCore.Http;

using backend.Application.Exceptions;
namespace backend.Application.Services;

public class WorkItemFileService : IWorkItemFileService
{
    private readonly IWorkItemFileRepository _fileRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly ISubWorkItemRepository _subWorkItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IWorkItemLockService _workItemLockService;
    private readonly IBoardLockService _boardLockService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;

    public WorkItemFileService(
        IWorkItemFileRepository fileRepo,
        IWorkItemRepository workItemRepo,
        ISubWorkItemRepository subWorkItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IWorkItemLockService workItemLockService,
        IBoardLockService boardLockService,
        IFileStorageService fileStorageService,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker)
    {
        _fileRepo = fileRepo;
        _workItemRepo = workItemRepo;
        _subWorkItemRepo = subWorkItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _workItemLockService = workItemLockService;
        _boardLockService = boardLockService;
        _fileStorageService = fileStorageService;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _commandInvoker = commandInvoker;
    }

    public async Task<WorkItemFileResponse> Upload(
        int workItemId,
        IFormFile file,
        int? subWorkItemId = null)
    {
        if (file == null || file.Length == 0)
            throw new ValidationException("File is required.");

        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);
        await _boardLockService.EnsureBoardAllowsMutationAsync(board.Id);
        await _workItemLockService.EnsureWorkItemNotLockedByOtherAsync(workItemId);

        var (fileUrl, thumbnailUrl) = await _fileStorageService.SaveWorkItemFileAsync(
            workItemId,
            file);

        var request = new WorkItemFileRequest
        {
            WorkItemId = workItemId,
            SubWorkItemId = subWorkItemId,
            FileName = file.FileName,
            FileType = file.ContentType,
            FileSize = file.Length,
            FileUrl = fileUrl,
            FileThumbnailUrl = thumbnailUrl,
        };

        return await Create(request);
    }

    public async Task<WorkItemFileResponse> Create(WorkItemFileRequest request)
    {
        var command = new AddWorkItemFileCommand(
            request,
            _userContext.GetUserId(),
            _fileRepo,
            _workItemRepo,
            _subWorkItemRepo,
            _boardRepo,
            _boardAccessService,
            _workItemLockService,
            _boardLockService,
            _domainEventSubject);

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<List<WorkItemFileResponse>> GetByWorkItemId(int workItemId)
    {
        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var files = await _fileRepo.GetByWorkItemIdAsync(workItemId);

        return files.Select(WorkItemFileHelper.ToResponse).ToList();
    }

    public async Task<List<WorkItemFileResponse>> GetBySubWorkItemId(int subWorkItemId)
    {
        var subWorkItem = await _subWorkItemRepo.GetById(subWorkItemId);
        if (subWorkItem == null)
            throw new NotFoundException("Sub work item does not exist.");

        var workItem = await _workItemRepo.GetById(subWorkItem.WorkItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var files = await _fileRepo.GetBySubWorkItemIdAsync(subWorkItemId);

        return files.Select(WorkItemFileHelper.ToResponse).ToList();
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> GetContentAsync(int fileId)
    {
        var file = await _fileRepo.GetById(fileId);
        if (file == null)
            throw new NotFoundException("File does not exist.");

        var workItem = await _workItemRepo.GetById(file.WorkItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var absolutePath = _fileStorageService.ResolveAbsolutePath(file.FileUrl);
        if (absolutePath == null)
            throw new NotFoundException("File content does not exist.");

        var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, file.FileType, file.FileName);
    }

    public async Task Delete(int fileId)
    {
        var command = new DeleteWorkItemFileCommand(
            fileId,
            _userContext.GetUserId(),
            _fileRepo,
            _workItemRepo,
            _subWorkItemRepo,
            _boardRepo,
            _boardAccessService,
            _workItemLockService,
            _boardLockService,
            _fileStorageService,
            _domainEventSubject);

        await _commandInvoker.ExecuteAsync(command);
    }
}
