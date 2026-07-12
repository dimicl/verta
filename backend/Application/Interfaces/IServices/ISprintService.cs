namespace backend.Application.Interfaces;

public interface ISprintService
{
    Task<SprintResponse> Create(SprintRequest request);
    Task<SprintResponse> GetById(int sprintId);
    Task<List<SprintResponse>> GetByBoardId(int boardId);
    Task<SprintResponse> Update(int sprintId, UpdateSprintRequest request);
    Task Delete(int sprintId);
}
