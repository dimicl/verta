namespace backend.Application.Interfaces;

public interface IWorkItemLockExpiryService
{
    Task PromoteNextInterestedAsync(int workItemId);
}