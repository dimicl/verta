public static class BoardHelper
{
    public static BoardResponse ToResponse(Board board)
    {
        return new BoardResponse
        {
            Id = board.Id,
            Name = board.Name,
            WorkspaceId = board.WorkspaceId,
            OwnerId = board.OwnerId,
            CreatedAt = board.CreatedAt
        };
    }
}