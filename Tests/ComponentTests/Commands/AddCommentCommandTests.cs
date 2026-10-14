using backend.Application.Exceptions;
using backend.Application.Interfaces;
using ComponentTests.Helpers;
using Moq;

namespace ComponentTests.Commands;

[TestFixture]
public class AddCommentCommandTests
{
    private Mock<ICommentRepository> _commentRepo = null!;
    private Mock<IWorkItemRepository> _workItemRepo = null!;
    private Mock<ISubWorkItemRepository> _subRepo = null!;
    private Mock<IBoardRepository> _boardRepo = null!;
    private Mock<IBoardAccessService> _access = null!;
    private Mock<IWorkItemLockService> _lock = null!;

    [SetUp]
    public void SetUp()
    {
        _commentRepo = new Mock<ICommentRepository>();
        _workItemRepo = new Mock<IWorkItemRepository>();
        _subRepo = new Mock<ISubWorkItemRepository>();
        _boardRepo = new Mock<IBoardRepository>();
        _access = new Mock<IBoardAccessService>();
        _lock = new Mock<IWorkItemLockService>();

        _workItemRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateWorkItem());
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());
        _lock.Setup(l => l.EnsureUserHasWriteLockAsync(1)).Returns(Task.CompletedTask);
    }

    private AddCommentCommand CreateSut(CommentRequest request, int userId = 1) =>
        new(
            request,
            userId,
            _commentRepo.Object,
            _workItemRepo.Object,
            _subRepo.Object,
            _boardRepo.Object,
            _access.Object,
            _lock.Object,
            TestDataFactory.EmptyEvents());

    [Test]
    public async Task Execute_ValidRequest_AddsComment()
    {
        var request = TestDataFactory.CreateCommentRequest();
        var withUser = TestDataFactory.CreateComment(id: 3);
        _commentRepo.Setup(r => r.Add(It.IsAny<Comment>()))
            .ReturnsAsync((Comment c) =>
            {
                c.Id = 3;
                return c;
            });
        _commentRepo.Setup(r => r.GetByIdWithUserAsync(3)).ReturnsAsync(withUser);

        var result = await CreateSut(request).ExecuteAsync();

        Assert.That(result.Id, Is.EqualTo(3));
        Assert.That(result.Content, Is.EqualTo("Test comment"));
        _lock.Verify(l => l.EnsureUserHasWriteLockAsync(1), Times.Once);
        _commentRepo.Verify(r => r.Add(It.Is<Comment>(
            c => c.WorkItemId == 1 && c.UserId == 1)), Times.Once);
    }

    [Test]
    public void Execute_EmptyContent_ThrowsValidation()
    {
        var request = TestDataFactory.CreateCommentRequest(content: "   ");

        var ex = Assert.ThrowsAsync<ValidationException>(() => CreateSut(request).ExecuteAsync());

        Assert.That(ex!.Message, Does.Contain("required"));
        _commentRepo.Verify(r => r.Add(It.IsAny<Comment>()), Times.Never);
    }

    [Test]
    public void Execute_WorkItemMissing_ThrowsNotFound()
    {
        var request = TestDataFactory.CreateCommentRequest(workItemId: 999);
        _workItemRepo.Setup(r => r.GetById(999)).ReturnsAsync((WorkItem?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => CreateSut(request).ExecuteAsync());

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void Execute_WithoutWriteLock_PropagatesForbidden()
    {
        var request = TestDataFactory.CreateCommentRequest();
        _lock.Setup(l => l.EnsureUserHasWriteLockAsync(1))
            .ThrowsAsync(new ForbiddenException("You must open the work item for editing before making changes."));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => CreateSut(request).ExecuteAsync());

        Assert.That(ex!.Message, Does.Contain("open the work item"));
    }
}
