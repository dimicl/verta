public static class WorkItemStateFactory
{
    public static IWorkItemState Create(WorkItemStatus status)
    {
        return status switch
        {
            WorkItemStatus.ToDo => new ToDoState(),
            WorkItemStatus.InProgress => new InProgressState(),
            WorkItemStatus.PR => new PRState(),
            WorkItemStatus.Testing => new TestingState(),
            WorkItemStatus.Done => new DoneState(),
            _ => throw new Exception("Invalid work item status.")
        };
    }
}