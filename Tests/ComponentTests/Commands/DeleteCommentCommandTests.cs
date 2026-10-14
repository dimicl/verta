using backend.Application.Exceptions;
using backend.Application.Interfaces;
using ComponentTests.Helpers;
using Moq;

namespace ComponentTests.Commands;

[TestFixture]
public class DeleteCommentCommandTests
{
    private Mock<ICommentRepository> _commentRepo = null!;
    private Mock<IWorkItemRepository> _workItemRepo = null!;
    private Mock<IBoardRepository> _boardRepo = null!;
    private Mock<IBoardAccessService> _access = null!;
    private Mock<IWorkItemLockService> _lock = null!;

    [SetUp]
    public void SetUp()
    {
        _commentRepo = new Mock<ICommentRepository>();
        _workItemRepo = new Mock<IWorkItemRepository>();
        _boardRepo = new Mock<IBoardRepository>();
        _access = new Mock<IBoardAccessService>();
        _lock = new Mock<IWorkItemLockService>();

        _workItemRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateWorkItem());
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());
        _lock.Setup(l => l.EnsureUserHasWriteLockAsync(1)).Returns(Task.CompletedTask);
    }

    private DeleteCommentCommand CreateSut(int commentId, int userId) =>
        new(
            commentId,
            userId,
            _commentRepo.Object,
            _workItemRepo.Object,
            _boardRepo.Object,
            _access.Object,
            _lock.Object,
            TestDataFactory.EmptyEvents());

    [Test]
    public async Task Execute_Owner_DeletesComment()
    {
        var comment = TestDataFactory.CreateComment(id: 1, userId: 1);
        _commentRepo.Setup(r => r.GetByIdWithUserAsync(1)).ReturnsAsync(comment);

        var result = await CreateSut(1, userId: 1).ExecuteAsync();

        Assert.That(result, Is.True);
        _commentRepo.Verify(r => r.Delete(comment), Times.Once);
    }

    [Test]
    public void Execute_NotOwner_ThrowsForbidden()
    {
        _commentRepo.Setup(r => r.GetByIdWithUserAsync(1))
            .ReturnsAsync(TestDataFactory.CreateComment(userId: 1));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => CreateSut(1, userId: 2).ExecuteAsync());

        Assert.That(ex!.Message, Does.Contain("only delete"));
        _commentRepo.Verify(r => r.Delete(It.IsAny<Comment>()), Times.Never);
    }

    [Test]
    public void Execute_Missing_ThrowsNotFound()
    {
        _commentRepo.Setup(r => r.GetByIdWithUserAsync(999)).ReturnsAsync((Comment?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => CreateSut(999, userId: 1).ExecuteAsync());

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }
}
