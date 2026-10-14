using backend.Application.Exceptions;
using backend.Application.Interfaces;
using backend.Application.Services;
using ComponentTests.Helpers;
using Moq;

namespace ComponentTests.Services;

[TestFixture]
public class WorkItemLockServiceTests
{
    private Mock<IWorkItemLockRepository> _lockRepo = null!;
    private Mock<IWorkItemLockInterestRepository> _interestRepo = null!;
    private Mock<IWorkItemRepository> _workItemRepo = null!;
    private Mock<IBoardRepository> _boardRepo = null!;
    private Mock<IBoardAccessService> _access = null!;
    private Mock<IUserContext> _user = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private WorkItemLockService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _lockRepo = new Mock<IWorkItemLockRepository>();
        _interestRepo = new Mock<IWorkItemLockInterestRepository>();
        _workItemRepo = new Mock<IWorkItemRepository>();
        _boardRepo = new Mock<IBoardRepository>();
        _access = new Mock<IBoardAccessService>();
        _user = new Mock<IUserContext>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _sut = new WorkItemLockService(
            _lockRepo.Object,
            _interestRepo.Object,
            _workItemRepo.Object,
            _boardRepo.Object,
            _access.Object,
            _user.Object,
            TestDataFactory.EmptyEvents(),
            _unitOfWork.Object);

        _workItemRepo.Setup(r => r.GetById(42))
            .ReturnsAsync(TestDataFactory.CreateWorkItem(42));
        _boardRepo.Setup(r => r.GetById(1))
            .ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());
    }

    [Test]
    public async Task Open_WhenNoLock_CreatesWriteLock()
    {
        _user.Setup(u => u.GetUserId()).Returns(1);
        _lockRepo.Setup(r => r.GetByWorkItemIdAsync(42)).ReturnsAsync((WorkItemLock?)null);
        _lockRepo.Setup(r => r.Add(It.IsAny<WorkItemLock>()))
            .ReturnsAsync((WorkItemLock l) =>
            {
                l.Id = 10;
                return l;
            });

        var result = await _sut.OpenWorkItem(42);

        Assert.Multiple(() =>
        {
            Assert.That(result.Mode, Is.EqualTo("WRITE"));
            Assert.That(result.UserId, Is.EqualTo(1));
            Assert.That(result.WorkItemId, Is.EqualTo(42));
            Assert.That(result.QueuePosition, Is.Null);
            Assert.That(result.ExpiresAt, Is.GreaterThan(result.LockedAt!.Value));
        });
        _lockRepo.Verify(r => r.Add(It.Is<WorkItemLock>(
            l => l.WorkItemId == 42 && l.LockedByUserId == 1)), Times.Once);
        _interestRepo.Verify(
            r => r.RegisterInterestAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task Open_WhenLockedByAnotherUser_ReturnsReadOnlyAndQueues()
    {
        _user.Setup(u => u.GetUserId()).Returns(2);
        _lockRepo.Setup(r => r.GetByWorkItemIdAsync(42))
            .ReturnsAsync(TestDataFactory.CreateLock(42, userId: 1));
        _interestRepo.Setup(r => r.GetPositionAsync(42, 2)).ReturnsAsync(1);

        var result = await _sut.OpenWorkItem(42);

        Assert.Multiple(() =>
        {
            Assert.That(result.Mode, Is.EqualTo("READ_ONLY"));
            Assert.That(result.UserId, Is.EqualTo(2));
            Assert.That(result.QueuePosition, Is.EqualTo(1));
        });
        _interestRepo.Verify(r => r.RegisterInterestAsync(42, 2), Times.Once);
        _lockRepo.Verify(r => r.Add(It.IsAny<WorkItemLock>()), Times.Never);
    }

    [Test]
    public async Task Open_WhenSameUserAlreadyHoldsLock_RefreshesWrite()
    {
        _user.Setup(u => u.GetUserId()).Returns(1);
        var existing = TestDataFactory.CreateLock(42, userId: 1,
            lockedAt: DateTime.UtcNow.AddMinutes(-2),
            expiresAt: DateTime.UtcNow.AddSeconds(5));
        _lockRepo.Setup(r => r.GetByWorkItemIdAsync(42)).ReturnsAsync(existing);

        var result = await _sut.OpenWorkItem(42);

        Assert.That(result.Mode, Is.EqualTo("WRITE"));
        Assert.That(result.LockedAt, Is.EqualTo(existing.LockedAt));
        Assert.That(result.ExpiresAt, Is.GreaterThan(DateTime.UtcNow.AddSeconds(10)));
        _lockRepo.Verify(r => r.Update(existing), Times.Once);
    }

    [Test]
    public async Task Open_WhenLockExpired_DeletesAndTakesWrite()
    {
        _user.Setup(u => u.GetUserId()).Returns(2);
        var expired = TestDataFactory.CreateLock(42, userId: 1,
            expiresAt: DateTime.UtcNow.AddSeconds(-5));
        _lockRepo.SetupSequence(r => r.GetByWorkItemIdAsync(42))
            .ReturnsAsync(expired);
        _lockRepo.Setup(r => r.Add(It.IsAny<WorkItemLock>()))
            .ReturnsAsync((WorkItemLock l) => l);

        var result = await _sut.OpenWorkItem(42);

        Assert.That(result.Mode, Is.EqualTo("WRITE"));
        Assert.That(result.UserId, Is.EqualTo(2));
        _lockRepo.Verify(r => r.Delete(expired), Times.Once);
    }

    [Test]
    public void Open_WhenWorkItemMissing_ThrowsNotFound()
    {
        _user.Setup(u => u.GetUserId()).Returns(1);
        _workItemRepo.Setup(r => r.GetById(999)).ReturnsAsync((WorkItem?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _sut.OpenWorkItem(999));

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void Open_WhenNoBoardAccess_ThrowsForbidden()
    {
        _user.Setup(u => u.GetUserId()).Returns(1);
        _lockRepo.Setup(r => r.GetByWorkItemIdAsync(42)).ReturnsAsync((WorkItemLock?)null);
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ThrowsAsync(new ForbiddenException("You do not have access to this board."));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _sut.OpenWorkItem(42));

        Assert.That(ex!.Message, Does.Contain("do not have access"));
    }

    [Test]
    public async Task Close_ByLockHolder_UnlocksAndPromotes()
    {
        _user.Setup(u => u.GetUserId()).Returns(1);
        var existing = TestDataFactory.CreateLock(42, userId: 1);
        _lockRepo.Setup(r => r.GetByWorkItemIdAsync(42)).ReturnsAsync(existing);
        _interestRepo.Setup(r => r.GetFirstAsync(42)).ReturnsAsync((WorkItemLockInterest?)null);
        _interestRepo.Setup(r => r.GetInterestedUserIdsAsync(42)).ReturnsAsync(new List<int>());

        var result = await _sut.CloseWorkItem(42);

        Assert.That(result.Mode, Is.EqualTo("UNLOCKED"));
        Assert.That(result.LockedAt, Is.Null);
        _lockRepo.Verify(r => r.Delete(existing), Times.Once);
    }

    [Test]
    public async Task Close_WithoutHoldingLock_ReturnsNoLockAndRemovesInterest()
    {
        _user.Setup(u => u.GetUserId()).Returns(2);
        _lockRepo.Setup(r => r.GetByWorkItemIdAsync(42))
            .ReturnsAsync(TestDataFactory.CreateLock(42, userId: 1));

        var result = await _sut.CloseWorkItem(42);

        Assert.That(result.Mode, Is.EqualTo("NO_LOCK"));
        Assert.That(result.UserId, Is.EqualTo(2));
        _interestRepo.Verify(r => r.RemoveUserAsync(42, 2), Times.Once);
        _lockRepo.Verify(r => r.Delete(It.IsAny<WorkItemLock>()), Times.Never);
    }

    [Test]
    public async Task Heartbeat_ByLockHolder_ExtendsExpiry()
    {
        _user.Setup(u => u.GetUserId()).Returns(1);
        var lockedAt = DateTime.UtcNow.AddMinutes(-1);
        var existing = TestDataFactory.CreateLock(42, userId: 1,
            lockedAt: lockedAt,
            expiresAt: DateTime.UtcNow.AddSeconds(5));
        _lockRepo.Setup(r => r.GetByWorkItemIdAsync(42)).ReturnsAsync(existing);

        var before = DateTime.UtcNow;
        var result = await _sut.Heartbeat(42);

        Assert.Multiple(() =>
        {
            Assert.That(result.Mode, Is.EqualTo("WRITE"));
            Assert.That(result.LockedAt, Is.EqualTo(lockedAt));
            Assert.That(result.ExpiresAt, Is.GreaterThan(before.AddSeconds(10)));
        });
        _lockRepo.Verify(r => r.Update(existing), Times.Once);
    }

    [Test]
    public void Heartbeat_WithoutLock_ThrowsForbidden()
    {
        _user.Setup(u => u.GetUserId()).Returns(1);
        _lockRepo.Setup(r => r.GetByWorkItemIdAsync(42)).ReturnsAsync((WorkItemLock?)null);

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _sut.Heartbeat(42));

        Assert.That(ex!.Message, Does.Contain("do not hold the lock"));
    }

    [Test]
    public void EnsureUserHasWriteLock_WhenLockedByOther_ThrowsForbidden()
    {
        _user.Setup(u => u.GetUserId()).Returns(2);
        _lockRepo.Setup(r => r.TryRefreshActiveLockAsync(42, 2, It.IsAny<DateTime>()))
            .ReturnsAsync(false);
        _lockRepo.Setup(r => r.GetByWorkItemIdAsync(42))
            .ReturnsAsync(TestDataFactory.CreateLock(42, userId: 1));

        var ex = Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.EnsureUserHasWriteLockAsync(42));

        Assert.That(ex!.Message, Does.Contain("locked by another"));
    }
}
