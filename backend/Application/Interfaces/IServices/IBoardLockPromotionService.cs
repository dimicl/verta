namespace backend.Application.Interfaces;

public interface IBoardLockPromotionService
{
    Task PromoteNextInQueueAsync(int boardId);
}