public class PRState : IWorkItemState
{
    public WorkItemStatus Status => WorkItemStatus.PR;

    public bool CanMoveTo(WorkItemStatus nextStatus)
    {
        return nextStatus == WorkItemStatus.Testing || nextStatus == WorkItemStatus.InProgress;
    }
}