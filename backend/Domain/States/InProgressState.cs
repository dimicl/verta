public class InProgressState : IWorkItemState
{
    public WorkItemStatus Status => WorkItemStatus.InProgress;

    public bool CanMoveTo(WorkItemStatus nextStatus)
    {
        return nextStatus == WorkItemStatus.PR || nextStatus == WorkItemStatus.ToDo;
    }
}