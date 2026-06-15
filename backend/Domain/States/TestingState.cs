public class TestingState : IWorkItemState
{
    public WorkItemStatus Status => WorkItemStatus.Testing;

    public bool CanMoveTo(WorkItemStatus nextStatus)
    {
        return nextStatus == WorkItemStatus.Done || nextStatus == WorkItemStatus.InProgress;
    }
}