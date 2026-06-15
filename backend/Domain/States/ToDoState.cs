public class ToDoState : IWorkItemState
{
    public WorkItemStatus Status => WorkItemStatus.ToDo;

    public bool CanMoveTo(WorkItemStatus nextStatus)
    {
        return nextStatus == WorkItemStatus.InProgress;
    }
}