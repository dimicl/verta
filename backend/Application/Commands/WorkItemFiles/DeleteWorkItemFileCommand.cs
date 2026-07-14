using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class DeleteWorkItemFileCommand : ICommand<bool>
{
    private readonly int _fileId;
    private readonly IWorkItemFileRepository _fileRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IFileStorageService _fileStorageService;
    private readonly DomainEventSubject _domainEventSubject;

    public DeleteWorkItemFileCommand(
        int fileId,
        IWorkItemFileRepository fileRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IFileStorageService fileStorageService,
        DomainEventSubject domainEventSubject)
    {
        _fileId = fileId;
        _fileRepo = fileRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _fileStorageService = fileStorageService;
        _domainEventSubject = domainEventSubject;
    }

    public async Task<bool> ExecuteAsync()
    {
        var file = await _fileRepo.GetById(_fileId);
        if (file == null)
            throw new NotFoundException("File does not exist.");

        var workItem = await _workItemRepo.GetById(file.WorkItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        await _fileStorageService.DeleteByUrlsAsync(file.FileUrl, file.FileThumbnailUrl);
        await _fileRepo.Delete(file);

        await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemFileDeleted, new
        {
            FileId = file.Id,
            WorkItemId = file.WorkItemId,
            BoardId = workItem.BoardId,
            FileName = file.FileName
        });

        return true;
    }
}
