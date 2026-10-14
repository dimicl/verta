using backend.Application.Interfaces;



using backend.Application.Exceptions;

public class DeleteWorkItemFileCommand : ICommand<bool>

{

    private readonly int _fileId;

    private readonly int _userId;

    private readonly IWorkItemFileRepository _fileRepo;

    private readonly IWorkItemRepository _workItemRepo;

    private readonly ISubWorkItemRepository _subWorkItemRepo;

    private readonly IBoardRepository _boardRepo;

    private readonly IBoardAccessService _boardAccessService;

    private readonly IWorkItemLockService _workItemLockService;

    private readonly IBoardLockService _boardLockService;

    private readonly IFileStorageService _fileStorageService;

    private readonly DomainEventSubject _domainEventSubject;



    public DeleteWorkItemFileCommand(

        int fileId,

        int userId,

        IWorkItemFileRepository fileRepo,

        IWorkItemRepository workItemRepo,

        ISubWorkItemRepository subWorkItemRepo,

        IBoardRepository boardRepo,

        IBoardAccessService boardAccessService,

        IWorkItemLockService workItemLockService,

        IBoardLockService boardLockService,

        IFileStorageService fileStorageService,

        DomainEventSubject domainEventSubject)

    {

        _fileId = fileId;

        _userId = userId;

        _fileRepo = fileRepo;

        _workItemRepo = workItemRepo;

        _subWorkItemRepo = subWorkItemRepo;

        _boardRepo = boardRepo;

        _boardAccessService = boardAccessService;

        _workItemLockService = workItemLockService;

        _boardLockService = boardLockService;

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

        await _boardLockService.EnsureBoardAllowsMutationAsync(board.Id);

        await _workItemLockService.EnsureWorkItemNotLockedByOtherAsync(file.WorkItemId);



        SubWorkItem? subWorkItem = null;

        if (file.SubWorkItemId.HasValue)

        {

            subWorkItem = await _subWorkItemRepo.GetById(file.SubWorkItemId.Value);

        }



        var fileName = file.FileName;

        var subWorkItemId = file.SubWorkItemId;



        await _fileStorageService.DeleteByUrlsAsync(file.FileUrl, file.FileThumbnailUrl);

        await _fileRepo.Delete(file);



        await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemFileDeleted, new

        {

            FileId = file.Id,

            WorkItemId = file.WorkItemId,

            BoardId = workItem.BoardId,

            FileName = fileName

        });



        var reporterId = subWorkItem?.UserId ?? workItem.CreatedByUserId;

        var assigneeId = subWorkItem?.AssignedUserId ?? workItem.AssignedUserId;

        var itemName = subWorkItem?.Name ?? workItem.Name;



        var fileTargets = WorkItemWatcherHelper.GetTargetUserIds(_userId, reporterId, assigneeId);

        if (fileTargets.Length > 0)

        {

            await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemActivityNotification, new

            {

                TargetUserIds = fileTargets,

                ActivityType = "WorkItemFileDeleted",

                WorkItemId = workItem.Id,

                SubWorkItemId = subWorkItemId,

                BoardId = workItem.BoardId,

                Name = itemName,

                FileName = fileName,

                UpdatedByUserId = _userId

            });

        }



        return true;

    }

}


