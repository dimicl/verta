using backend.Application.Exceptions;
using backend.Application.Interfaces;
using backend.Application.Services;
using ComponentTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ComponentTests.Services;

[TestFixture]
public class CommentServiceTests
{
    private Mock<ICommentRepository> _commentRepo = null!;
    private Mock<IWorkItemRepository> _workItemRepo = null!;
    private Mock<ISubWorkItemRepository> _subRepo = null!;
    private Mock<IBoardRepository> _boardRepo = null!;
    private Mock<IBoardAccessService> _access = null!;
    private Mock<IWorkItemLockService> _lock = null!;
    private Mock<IUserContext> _user = null!;
    private CommentService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _commentRepo = new Mock<ICommentRepository>();
        _workItemRepo = new Mock<IWorkItemRepository>();
        _subRepo = new Mock<ISubWorkItemRepository>();
        _boardRepo = new Mock<IBoardRepository>();
        _access = new Mock<IBoardAccessService>();
        _lock = new Mock<IWorkItemLockService>();
        _user = new Mock<IUserContext>();

        _sut = new CommentService(
            _commentRepo.Object,
            _workItemRepo.Object,
            _subRepo.Object,
            _boardRepo.Object,
            _access.Object,
            _lock.Object,
            _user.Object,
            TestDataFactory.EmptyEvents(),
            new CommandInvoker(NullLogger<CommandInvoker>.Instance));
    }

    [Test]
    public async Task GetByWorkItemId_ReturnsMappedComments()
    {
        _workItemRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateWorkItem());
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());
        _commentRepo.Setup(r => r.GetByWorkItemIdAsync(1))
            .ReturnsAsync(TestDataFactory.CreateCommentEntities(1, 2, 3));

        var result = await _sut.GetByWorkItemId(1);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Select(c => c.Id), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(result.Select(c => c.WorkItemId), Is.All.EqualTo(1));
        Assert.That(result.Select(c => c.Content), Has.All.Not.Empty);
    }

    [Test]
    public async Task GetByWorkItemId_Empty_ReturnsEmptyList()
    {
        _workItemRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateWorkItem());
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());
        _commentRepo.Setup(r => r.GetByWorkItemIdAsync(1)).ReturnsAsync(new List<Comment>());

        var result = await _sut.GetByWorkItemId(1);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetByWorkItemId_MissingWorkItem_ThrowsNotFound()
    {
        _workItemRepo.Setup(r => r.GetById(999)).ReturnsAsync((WorkItem?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByWorkItemId(999));

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }
}
