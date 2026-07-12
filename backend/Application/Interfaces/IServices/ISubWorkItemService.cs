namespace backend.Application.Interfaces;

public interface ISubWorkItemService
{
    Task<SubWorkItemResponse> Create(SubWorkItemRequest request);
    Task<SubWorkItemResponse> GetById(int subWorkItemId);
    Task<List<SubWorkItemResponse>> GetByWorkItemId(int workItemId);
    Task<SubWorkItemResponse> Update(int subWorkItemId, UpdateSubWorkItemRequest request);
    Task<SubWorkItemResponse> ChangeStatus(int subWorkItemId, ChangeSubWorkItemStatusRequest request);
    Task Delete(int subWorkItemId);
}