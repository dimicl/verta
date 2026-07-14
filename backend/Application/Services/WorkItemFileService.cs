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
    private readonly IFileStorageService _fileStorageService;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;

    public WorkItemFileService(
        IWorkItemFileRepository fileRepo,
        IWorkItemRepository workItemRepo,
        ISubWorkItemRepository subWorkItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IFileStorageService fileStorageService,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker)
    {
        _fileRepo = fileRepo;
        _workItemRepo = workItemRepo;
        _subWorkItemRepo = subWorkItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _fileStorageService = fileStorageService;
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
            _fileRepo,
            _workItemRepo,
            _subWorkItemRepo,
            _boardRepo,
            _boardAccessService,
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

    public async Task Delete(int fileId)
    {
        var command = new DeleteWorkItemFileCommand(
            fileId,
            _fileRepo,
            _workItemRepo,
            _boardRepo,
            _boardAccessService,
            _fileStorageService,
            _domainEventSubject);

        await _commandInvoker.ExecuteAsync(command);
    }
}
