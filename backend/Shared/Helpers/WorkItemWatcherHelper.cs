public static class WorkItemWatcherHelper
{
    /// <summary>
    /// Reporter + assignee, excluding the user who performed the action.
    /// </summary>
    public static int[] GetTargetUserIds(int actorUserId, int reporterUserId, int? assignedUserId)
    {
        var ids = new HashSet<int>();

        if (reporterUserId > 0 && reporterUserId != actorUserId)
            ids.Add(reporterUserId);

        if (assignedUserId is int assignee && assignee > 0 && assignee != actorUserId)
            ids.Add(assignee);

        return ids.ToArray();
    }
}
