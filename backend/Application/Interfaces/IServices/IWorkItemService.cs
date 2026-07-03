namespace backend.Application.Interfaces;

public interface IWorkItemService
{
    Task<WorkItemResponse> Create(WorkItemRequest request);
    Task<List<WorkItemResponse>> GetByBoardId(int boardId);
    Task<List<WorkItemResponse>> GetBySprintId(int sprintId);
    Task<WorkItemResponse> GetById(int workItemId);
    Task<WorkItemResponse> ChangeStatus(int workItemId, ChangeWorkItemStatusRequest request);
    Task<WorkItemResponse> ChangePriority(int workItemId, ChangeWorkItemPriorityRequest request);
    Task<WorkItemResponse> ChangeAssignee(int workItemId, ChangeWorkItemAssigneeRequest request);
    Task<WorkItemResponse> Update(int workItemId, WorkItemRequest request);
    Task Delete(int workItemId);
}