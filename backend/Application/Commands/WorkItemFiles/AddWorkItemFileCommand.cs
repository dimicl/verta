using backend.Application.Interfaces;



using backend.Application.Exceptions;

public class AddWorkItemFileCommand : ICommand<WorkItemFileResponse>

{

    private readonly WorkItemFileRequest _request;

    private readonly int _userId;

    private readonly IWorkItemFileRepository _fileRepo;

    private readonly IWorkItemRepository _workItemRepo;

    private readonly ISubWorkItemRepository _subWorkItemRepo;

    private readonly IBoardRepository _boardRepo;

    private readonly IBoardAccessService _boardAccessService;

    private readonly IWorkItemLockService _workItemLockService;

    private readonly IBoardLockService _boardLockService;

    private readonly DomainEventSubject _domainEventSubject;



    public AddWorkItemFileCommand(

        WorkItemFileRequest request,

        int userId,

        IWorkItemFileRepository fileRepo,

        IWorkItemRepository workItemRepo,

        ISubWorkItemRepository subWorkItemRepo,

        IBoardRepository boardRepo,

        IBoardAccessService boardAccessService,

        IWorkItemLockService workItemLockService,

        IBoardLockService boardLockService,

        DomainEventSubject domainEventSubject)

    {

        _request = request;

        _userId = userId;

        _fileRepo = fileRepo;

        _workItemRepo = workItemRepo;

        _subWorkItemRepo = subWorkItemRepo;

        _boardRepo = boardRepo;

        _boardAccessService = boardAccessService;

        _workItemLockService = workItemLockService;

        _boardLockService = boardLockService;

        _domainEventSubject = domainEventSubject;

    }



    public async Task<WorkItemFileResponse> ExecuteAsync()

    {

        if (_request == null)

            throw new ValidationException("Request not found.");



        if (string.IsNullOrWhiteSpace(_request.FileName))

            throw new ValidationException("File name is required.");



        if (string.IsNullOrWhiteSpace(_request.FileType))

            throw new ValidationException("File type is required.");



        if (string.IsNullOrWhiteSpace(_request.FileUrl))

            throw new ValidationException("File URL is required.");



        if (_request.FileSize <= 0)

            throw new ValidationException("File size must be greater than zero.");



        var expectedPrefix = $"/uploads/work-items/{_request.WorkItemId}/";

        if (!_request.FileUrl.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))

            throw new ValidationException("File URL is not valid for this work item.");



        if (!string.IsNullOrWhiteSpace(_request.FileThumbnailUrl) &&

            !_request.FileThumbnailUrl.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))

        {

            throw new ValidationException("Thumbnail URL is not valid for this work item.");

        }



        var workItem = await _workItemRepo.GetById(_request.WorkItemId);

        if (workItem == null)

            throw new NotFoundException("Work item does not exist.");



        var board = await _boardRepo.GetById(workItem.BoardId);

        if (board == null)

            throw new NotFoundException("Board does not exist.");



        await _boardAccessService.EnsureBoardAccessAsync(board);

        await _boardLockService.EnsureBoardAllowsMutationAsync(board.Id);

        await _workItemLockService.EnsureWorkItemNotLockedByOtherAsync(_request.WorkItemId);



        SubWorkItem? subWorkItem = null;

        if (_request.SubWorkItemId.HasValue)

        {

            subWorkItem = await _subWorkItemRepo.GetById(_request.SubWorkItemId.Value);

            if (subWorkItem == null)

                throw new NotFoundException("Sub work item does not exist.");



            if (subWorkItem.WorkItemId != _request.WorkItemId)

                throw new ValidationException("Sub work item does not belong to this work item.");

        }



        var file = new WorkItemFile

        {

            WorkItemId = _request.WorkItemId,

            SubWorkItemId = _request.SubWorkItemId,

            FileName = _request.FileName,

            FileType = _request.FileType,

            FileSize = _request.FileSize,

            FileUrl = _request.FileUrl,

            FileThumbnailUrl = _request.FileThumbnailUrl,

            CreatedAt = DateTime.UtcNow,

        };



        var created = await _fileRepo.Add(file);



        await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemFileAdded, new

        {

            FileId = created.Id,

            WorkItemId = created.WorkItemId,

            BoardId = workItem.BoardId,

            FileName = created.FileName,

            FileType = created.FileType,

            FileSize = created.FileSize,

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

                ActivityType = "WorkItemFileAdded",

                WorkItemId = workItem.Id,

                SubWorkItemId = created.SubWorkItemId,

                BoardId = workItem.BoardId,

                Name = itemName,

                FileName = created.FileName,

                UpdatedByUserId = _userId

            });

        }



        return WorkItemFileHelper.ToResponse(created);

    }

}


