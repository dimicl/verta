namespace backend.Application.Interfaces;

public interface IWorkItemService
{
    Task<WorkItemResponse> Create(WorkItemRequest request);
    Task<List<WorkItemResponse>> GetByBoardId(int boardId);
    Task<WorkItemResponse> GetById(int workItemId);
    Task<WorkItemResponse> ChangeStatus(int workItemId, ChangeWorkItemStatusRequest request);
}