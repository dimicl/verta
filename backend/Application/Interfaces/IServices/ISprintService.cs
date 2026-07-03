namespace backend.Application.Interfaces;

public interface ISprintService
{
    Task<SprintResponse> Create(SprintRequest request);
    Task<SprintResponse> GetById(int sprintId);
    Task<List<SprintResponse>> GetByBoardId(int boardId);
}
