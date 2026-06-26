namespace backend.Application.Interfaces;

public interface IWorkItemLockExpiryService
{
    Task NotifyAndClearInterestsAsync(int workItemId);
}