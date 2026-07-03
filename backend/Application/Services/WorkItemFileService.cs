using backend.Application.Interfaces;
using backend.Shared.Helpers;
using Microsoft.AspNetCore.Http;

namespace backend.Application.Services;

public class WorkItemFileService : IWorkItemFileService
{
    private readonly IWorkItemFileRepository _fileRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IFileStorageService _fileStorageService;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly CommandInvoker _commandInvoker;

    public WorkItemFileService(
        IWorkItemFileRepository fileRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IFileStorageService fileStorageService,
        DomainEventSubject domainEventSubject,
        CommandInvoker commandInvoker)
    {
        _fileRepo = fileRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _fileStorageService = fileStorageService;
        _domainEventSubject = domainEventSubject;
        _commandInvoker = commandInvoker;
    }

    public async Task<WorkItemFileResponse> Upload(int workItemId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new Exception("File is required.");

        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var (fileUrl, thumbnailUrl) = await _fileStorageService.SaveWorkItemFileAsync(
            workItemId,
            file);

        var request = new WorkItemFileRequest
        {
            WorkItemId = workItemId,
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

        var workItem = await _workItemRepo.GetById(request.WorkItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var file = new WorkItemFile
        {
            WorkItemId = request.WorkItemId,
            FileName = request.FileName,
            FileType = request.FileType,
            FileSize = request.FileSize,
            FileUrl = request.FileUrl,
            FileThumbnailUrl = request.FileThumbnailUrl,
            CreatedAt = DateTime.UtcNow,
        };

        var command = new AddWorkItemFileCommand(async () =>
        {
            var created = await _fileRepo.Add(file);

            await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemFileAdded, new
            {
                FileId = created.Id,
                WorkItemId = created.WorkItemId,
                FileName = created.FileName,
                FileType = created.FileType,
                FileSize = created.FileSize,
            });

            return WorkItemFileHelper.ToResponse(created);
        });

        return await _commandInvoker.ExecuteAsync(command);
    }

    public async Task<List<WorkItemFileResponse>> GetByWorkItemId(int workItemId)
    {
        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var files = await _fileRepo.GetByWorkItemIdAsync(workItemId);

        return files.Select(WorkItemFileHelper.ToResponse).ToList();
    }

    public async Task Delete(int fileId)
    {
        var file = await _fileRepo.GetById(fileId);
        if (file == null)
            throw new Exception("File does not exist.");

        var workItem = await _workItemRepo.GetById(file.WorkItemId);
        if (workItem == null)
            throw new Exception("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var command = new DeleteWorkItemFileCommand(async () =>
        {
            await _fileStorageService.DeleteByUrlsAsync(file.FileUrl, file.FileThumbnailUrl);
            await _fileRepo.Delete(file);

            await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemFileDeleted, new
            {
                FileId = file.Id,
                WorkItemId = file.WorkItemId,
                FileName = file.FileName
            });

            return true;
        });

        await _commandInvoker.ExecuteAsync(command);
    }
}
