namespace backend.Application.Interfaces;

public interface ISubWorkItemService
{
    Task<SubWorkItemResponse> Create(SubWorkItemRequest request);
    Task<List<SubWorkItemResponse>> GetByWorkItemId(int workItemId);
    Task<SubWorkItemResponse> ChangeStatus(int subWorkItemId, ChangeSubWorkItemStatusRequest request);
}