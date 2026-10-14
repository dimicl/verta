using backend.Application.Exceptions;
using backend.Application.Interfaces;
using backend.Application.Services;
using ComponentTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ComponentTests.Services;

[TestFixture]
public class WorkItemServiceTests
{
    private Mock<IWorkItemRepository> _workItemRepo = null!;
    private Mock<IBoardRepository> _boardRepo = null!;
    private Mock<ISprintRepository> _sprintRepo = null!;
    private Mock<IWorkspaceMemberRepository> _memberRepo = null!;
    private Mock<IBoardAccessService> _access = null!;
    private Mock<IUserContext> _user = null!;
    private Mock<IWorkItemLockService> _workItemLock = null!;
    private Mock<IBoardLockService> _boardLock = null!;
    private WorkItemService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _workItemRepo = new Mock<IWorkItemRepository>();
        _boardRepo = new Mock<IBoardRepository>();
        _sprintRepo = new Mock<ISprintRepository>();
        _memberRepo = new Mock<IWorkspaceMemberRepository>();
        _access = new Mock<IBoardAccessService>();
        _user = new Mock<IUserContext>();
        _workItemLock = new Mock<IWorkItemLockService>();
        _boardLock = new Mock<IBoardLockService>();

        _sut = new WorkItemService(
            _workItemRepo.Object,
            _boardRepo.Object,
            _sprintRepo.Object,
            _memberRepo.Object,
            _access.Object,
            _user.Object,
            TestDataFactory.EmptyEvents(),
            new CommandInvoker(NullLogger<CommandInvoker>.Instance),
            _workItemLock.Object,
            _boardLock.Object);
    }

    [Test]
    public async Task GetById_Existing_ReturnsMappedResponse()
    {
        var workItem = TestDataFactory.CreateWorkItem(5);
        _workItemRepo.Setup(r => r.GetById(5)).ReturnsAsync(workItem);
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());

        var result = await _sut.GetById(5);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(5));
            Assert.That(result.Name, Is.EqualTo(workItem.Name));
            Assert.That(result.BoardId, Is.EqualTo(1));
        });
        _access.Verify(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()), Times.Once);
    }

    [Test]
    public void GetById_Missing_ThrowsNotFound()
    {
        _workItemRepo.Setup(r => r.GetById(999)).ReturnsAsync((WorkItem?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _sut.GetById(999));

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void GetById_NoAccess_PropagatesForbidden()
    {
        _workItemRepo.Setup(r => r.GetById(1))
            .ReturnsAsync(TestDataFactory.CreateWorkItem());
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ThrowsAsync(new ForbiddenException("You do not have access to this board."));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _sut.GetById(1));

        Assert.That(ex!.Message, Does.Contain("do not have access"));
    }

    [Test]
    public async Task GetByBoardId_ReturnsMappedItems()
    {
        var items = TestDataFactory.CreateWorkItemEntities(1, 2, 3);
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());
        _workItemRepo.Setup(r => r.GetByBoardIdAsync(1)).ReturnsAsync(items);

        var result = await _sut.GetByBoardId(1);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Select(i => i.Id), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(result.Select(i => i.BoardId), Is.All.EqualTo(1));
    }

    [Test]
    public async Task GetByBoardId_EmptyBoard_ReturnsEmptyList()
    {
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());
        _workItemRepo.Setup(r => r.GetByBoardIdAsync(1)).ReturnsAsync(new List<WorkItem>());

        var result = await _sut.GetByBoardId(1);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetByBoardId_BoardMissing_ThrowsNotFound()
    {
        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync((Board?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByBoardId(1));
    }
}
