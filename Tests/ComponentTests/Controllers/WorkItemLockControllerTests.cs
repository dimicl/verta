using backend.Application.Exceptions;
using backend.Application.Interfaces;
using ComponentTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ComponentTests.Controllers;

[TestFixture]
public class WorkItemLockControllerTests
{
    private Mock<IWorkItemLockService> _serviceMock = null!;
    private WorkItemLockController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IWorkItemLockService>();
        _controller = new WorkItemLockController(_serviceMock.Object);
    }

    private static WorkItemLockResponse ExtractLockResponse(IActionResult result)
    {
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.TypeOf<WorkItemLockResponse>());
        return (WorkItemLockResponse)ok.Value!;
    }

  #region Open

    [Test]
    public async Task OpenWorkItemFirstUserReturnsWriteLock()
    {
        var expected = TestDataFactory.CreateWriteLockResponse(workItemId: 42, userId: 1);
        _serviceMock.Setup(s => s.OpenWorkItem(42)).ReturnsAsync(expected);

        var lockResponse = ExtractLockResponse(await _controller.OpenWorkItem(42));

        Assert.Multiple(() =>
        {
            Assert.That(lockResponse.Mode, Is.EqualTo("WRITE"));
            Assert.That(lockResponse.UserId, Is.EqualTo(1));
            Assert.That(lockResponse.WorkItemId, Is.EqualTo(42));
            Assert.That(lockResponse.LockedAt, Is.Not.Null.And.LessThan(DateTime.UtcNow.AddSeconds(1)));
            Assert.That(lockResponse.ExpiresAt, Is.GreaterThan(lockResponse.LockedAt!.Value));
            Assert.That(lockResponse.QueuePosition, Is.Null);
        });
    }

    [Test]
    public async Task OpenWorkItemWhileLockedByAnotherUserReturnsReadOnly()
    {
        var expected = TestDataFactory.CreateReadOnlyLockResponse(workItemId: 42, userId: 2, queuePosition: 1);
        _serviceMock.Setup(s => s.OpenWorkItem(42)).ReturnsAsync(expected);

        var lockResponse = ExtractLockResponse(await _controller.OpenWorkItem(42));

        Assert.Multiple(() =>
        {
            Assert.That(lockResponse.Mode, Is.EqualTo("READ_ONLY"));
            Assert.That(lockResponse.UserId, Is.EqualTo(2));
            Assert.That(lockResponse.QueuePosition, Is.EqualTo(1));
            Assert.That(lockResponse.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
        });
    }

    [Test]
    public async Task OpenWorkItemTwoUsersScenarioReturnsExpectedModes()
    {
        const int workItemId = 42;
        var modes = new List<string>();

        _serviceMock.SetupSequence(s => s.OpenWorkItem(workItemId))
            .ReturnsAsync(TestDataFactory.CreateWriteLockResponse(workItemId, userId: 1))
            .ReturnsAsync(TestDataFactory.CreateReadOnlyLockResponse(workItemId, userId: 2, queuePosition: 1))
            .ReturnsAsync(TestDataFactory.CreateReadOnlyLockResponse(workItemId, userId: 3, queuePosition: 2));

        for (var user = 1; user <= 3; user++)
        {
            modes.Add(ExtractLockResponse(await _controller.OpenWorkItem(workItemId)).Mode);
        }

        CollectionAssert.AreEqual(new[] { "WRITE", "READ_ONLY", "READ_ONLY" }, modes);
        Assert.That(modes, Has.None.EqualTo("UNLOCKED"));
    }

  #endregion

  #region Close

    [Test]
    public async Task CloseWorkItemByLockHolderReturnsUnlocked()
    {
        var expected = TestDataFactory.CreateUnlockedResponse(workItemId: 42, userId: 1);
        _serviceMock.Setup(s => s.CloseWorkItem(42)).ReturnsAsync(expected);

        var lockResponse = ExtractLockResponse(await _controller.CloseWorkItem(42));

        Assert.Multiple(() =>
        {
            Assert.That(lockResponse.Mode, Is.EqualTo("UNLOCKED"));
            Assert.That(lockResponse.LockedAt, Is.Null);
            Assert.That(lockResponse.ExpiresAt, Is.Null);
        });
    }

    [Test]
    public async Task CloseWorkItemWithoutLockReturnsNoLock()
    {
        var expected = TestDataFactory.CreateNoLockResponse(workItemId: 42, userId: 2);
        _serviceMock.Setup(s => s.CloseWorkItem(42)).ReturnsAsync(expected);

        var lockResponse = ExtractLockResponse(await _controller.CloseWorkItem(42));

        Assert.That(lockResponse.Mode, Is.EqualTo("NO_LOCK"));
    }

    [Test]
    public async Task CloseWorkItemWhileAnotherUserHoldsLockReturnsNoLock()
    {
        var expected = TestDataFactory.CreateNoLockResponse(workItemId: 42, userId: 2);
        _serviceMock.Setup(s => s.CloseWorkItem(42)).ReturnsAsync(expected);

        var lockResponse = ExtractLockResponse(await _controller.CloseWorkItem(42));

        Assert.Multiple(() =>
        {
            Assert.That(lockResponse.Mode, Is.EqualTo("NO_LOCK"));
            Assert.That(lockResponse.UserId, Is.EqualTo(2));
            Assert.That(lockResponse.LockedAt, Is.Null);
            Assert.That(lockResponse.ExpiresAt, Is.Null);
        });
    }

  #endregion

  #region Heartbeat

    [Test]
    public async Task HeartbeatByLockHolderExtendsSession()
    {
        var before = DateTime.UtcNow.AddSeconds(5);
        var expected = TestDataFactory.CreateWriteLockResponse(
            workItemId: 42,
            userId: 1,
            expiresAt: DateTime.UtcNow.AddSeconds(30));
        _serviceMock.Setup(s => s.Heartbeat(42)).ReturnsAsync(expected);

        var lockResponse = ExtractLockResponse(await _controller.Heartbeat(42));

        Assert.Multiple(() =>
        {
            Assert.That(lockResponse.Mode, Is.EqualTo("WRITE"));
            Assert.That(lockResponse.ExpiresAt, Is.GreaterThan(before));
        });
    }

    [Test]
    public void HeartbeatWithoutLockThrowsForbidden()
    {
        _serviceMock.Setup(s => s.Heartbeat(42))
            .ThrowsAsync(new ForbiddenException("You do not hold the lock for this work item."));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _controller.Heartbeat(42));

        Assert.That(ex!.Message, Does.Contain("do not hold the lock"));
    }

    [Test]
    public async Task HeartbeatPreservesLockedAtWhileExtendingExpiry()
    {
        var lockedAt = DateTime.UtcNow.AddMinutes(-1);
        var expected = TestDataFactory.CreateWriteLockResponse(
            workItemId: 42,
            userId: 1,
            lockedAt: lockedAt,
            expiresAt: DateTime.UtcNow.AddSeconds(30));
        _serviceMock.Setup(s => s.Heartbeat(42)).ReturnsAsync(expected);

        var lockResponse = ExtractLockResponse(await _controller.Heartbeat(42));

        Assert.Multiple(() =>
        {
            Assert.That(lockResponse.Mode, Is.EqualTo("WRITE"));
            Assert.That(lockResponse.LockedAt, Is.EqualTo(lockedAt));
            Assert.That(lockResponse.ExpiresAt, Is.GreaterThan(lockedAt));
        });
    }

  #endregion
}
