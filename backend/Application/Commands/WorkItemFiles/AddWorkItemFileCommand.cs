using backend.Application.Interfaces;

using backend.Application.Exceptions;
public class AddWorkItemFileCommand : ICommand<WorkItemFileResponse>
{
    private readonly WorkItemFileRequest _request;
    private readonly IWorkItemFileRepository _fileRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly ISubWorkItemRepository _subWorkItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly DomainEventSubject _domainEventSubject;

    public AddWorkItemFileCommand(
        WorkItemFileRequest request,
        IWorkItemFileRepository fileRepo,
        IWorkItemRepository workItemRepo,
        ISubWorkItemRepository subWorkItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        DomainEventSubject domainEventSubject)
    {
        _request = request;
        _fileRepo = fileRepo;
        _workItemRepo = workItemRepo;
        _subWorkItemRepo = subWorkItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
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

        var workItem = await _workItemRepo.GetById(_request.WorkItemId);
        if (workItem == null)
            throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        if (_request.SubWorkItemId.HasValue)
        {
            var subWorkItem = await _subWorkItemRepo.GetById(_request.SubWorkItemId.Value);
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

        return WorkItemFileHelper.ToResponse(created);
    }
}
