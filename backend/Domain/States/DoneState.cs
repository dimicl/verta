public class DoneState : IWorkItemState
{
    public WorkItemStatus Status => WorkItemStatus.Done;

    public bool CanMoveTo(WorkItemStatus nextStatus)
    {
        return false;
    }
}