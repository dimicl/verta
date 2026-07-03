using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class SprintService : ISprintService
{
    private readonly ISprintRepository _sprintRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IUserContext _userContext;

    public SprintService(
        ISprintRepository sprintRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IUserContext userContext)
    {
        _sprintRepo = sprintRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _userContext = userContext;
    }

    public async Task<SprintResponse> Create(SprintRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("Sprint name is required.");

        var board = await _boardRepo.GetById(request.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var startDate = ToUtcDate(request.StartDate);
        var endDate = ToUtcDate(request.EndDate);

        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
            throw new Exception("End date must be on or after start date.");

        var sprint = new Sprint
        {
            Name = request.Name.Trim(),
            BoardId = request.BoardId,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow
        };

        var createdSprint = await _sprintRepo.Add(sprint);

        return SprintHelper.ToResponse(createdSprint);
    }

    public async Task<SprintResponse> GetById(int sprintId)
    {
        var sprint = await _sprintRepo.GetById(sprintId);
        if (sprint == null)
            throw new Exception("Sprint does not exist.");

        var board = await _boardRepo.GetById(sprint.BoardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        return SprintHelper.ToResponse(sprint);
    }

    public async Task<List<SprintResponse>> GetByBoardId(int boardId)
    {
        var board = await _boardRepo.GetById(boardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        var sprints = await _sprintRepo.GetByBoardIdAsync(boardId);

        return sprints.Select(SprintHelper.ToResponse).ToList();
    }

    private static DateTime? ToUtcDate(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        var date = value.Value;

        return date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc),
        };
    }
}
