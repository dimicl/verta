public interface IWorkItemState
{
    WorkItemStatus Status { get; }

    bool CanMoveTo(WorkItemStatus nextStatus);
}