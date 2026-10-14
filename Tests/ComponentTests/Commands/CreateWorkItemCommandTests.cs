using backend.Application.Exceptions;
using backend.Application.Interfaces;
using ComponentTests.Helpers;
using Moq;

namespace ComponentTests.Commands;

[TestFixture]
public class CreateWorkItemCommandTests
{
    private Mock<IWorkItemRepository> _workItemRepo = null!;
    private Mock<IBoardRepository> _boardRepo = null!;
    private Mock<ISprintRepository> _sprintRepo = null!;
    private Mock<IWorkspaceMemberRepository> _memberRepo = null!;
    private Mock<IBoardAccessService> _access = null!;
    private Mock<IBoardLockService> _boardLock = null!;

    [SetUp]
    public void SetUp()
    {
        _workItemRepo = new Mock<IWorkItemRepository>();
        _boardRepo = new Mock<IBoardRepository>();
        _sprintRepo = new Mock<ISprintRepository>();
        _memberRepo = new Mock<IWorkspaceMemberRepository>();
        _access = new Mock<IBoardAccessService>();
        _boardLock = new Mock<IBoardLockService>();

        _boardRepo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateBoard());
        _access.Setup(a => a.EnsureBoardAccessAsync(It.IsAny<Board>()))
            .ReturnsAsync(TestDataFactory.CreateMember());
        _boardLock.Setup(b => b.EnsureBoardAllowsMutationAsync(1)).Returns(Task.CompletedTask);
    }

    private CreateWorkItemCommand CreateSut(WorkItemRequest request, int userId = 1) =>
        new(
            request,
            userId,
            _workItemRepo.Object,
            _boardRepo.Object,
            _sprintRepo.Object,
            _memberRepo.Object,
            _access.Object,
            _boardLock.Object,
            TestDataFactory.EmptyEvents());

    [Test]
    public async Task Execute_ValidRequest_AddsWorkItemAndReturnsResponse()
    {
        var request = TestDataFactory.CreateWorkItemRequest();
        _workItemRepo.Setup(r => r.Add(It.IsAny<WorkItem>()))
            .ReturnsAsync((WorkItem w) =>
            {
                w.Id = 7;
                return w;
            });

        var result = await CreateSut(request).ExecuteAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(7));
            Assert.That(result.Name, Is.EqualTo(request.Name));
            Assert.That(result.BoardId, Is.EqualTo(1));
            Assert.That(result.Status, Is.EqualTo(WorkItemStatus.ToDo));
            Assert.That(result.CreatedByUserId, Is.EqualTo(1));
        });
        _workItemRepo.Verify(r => r.Add(It.Is<WorkItem>(
            w => w.Name == request.Name && w.BoardId == 1 && w.CreatedByUserId == 1)), Times.Once);
        _boardLock.Verify(b => b.EnsureBoardAllowsMutationAsync(1), Times.Once);
    }

    [Test]
    public void Execute_MissingName_ThrowsValidation()
    {
        var request = TestDataFactory.CreateWorkItemRequest(name: "  ");

        var ex = Assert.ThrowsAsync<ValidationException>(() => CreateSut(request).ExecuteAsync());

        Assert.That(ex!.Message, Does.Contain("name is required"));
        _workItemRepo.Verify(r => r.Add(It.IsAny<WorkItem>()), Times.Never);
    }

    [Test]
    public void Execute_MissingDescription_ThrowsValidation()
    {
        var request = TestDataFactory.CreateWorkItemRequest(description: "");

        var ex = Assert.ThrowsAsync<ValidationException>(() => CreateSut(request).ExecuteAsync());

        Assert.That(ex!.Message, Does.Contain("description is required"));
    }

    [Test]
    public void Execute_BoardMissing_ThrowsNotFound()
    {
        var request = TestDataFactory.CreateWorkItemRequest(boardId: 999);
        _boardRepo.Setup(r => r.GetById(999)).ReturnsAsync((Board?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => CreateSut(request).ExecuteAsync());

        Assert.That(ex!.Message, Does.Contain("Board does not exist"));
    }

    [Test]
    public void Execute_SprintOnWrongBoard_ThrowsValidation()
    {
        var request = TestDataFactory.CreateWorkItemRequest() with { SprintId = 5 };
        _sprintRepo.Setup(r => r.GetById(5))
            .ReturnsAsync(TestDataFactory.CreateSprint(id: 5, boardId: 99));

        var ex = Assert.ThrowsAsync<ValidationException>(() => CreateSut(request).ExecuteAsync());

        Assert.That(ex!.Message, Does.Contain("does not belong to this board"));
    }

    [Test]
    public void Execute_AssigneeNotWorkspaceMember_ThrowsForbidden()
    {
        var request = TestDataFactory.CreateWorkItemRequest() with { AssignedUserId = 9 };
        _memberRepo.Setup(r => r.GetByWorkspaceAndUserIdAsync(1, 9))
            .ReturnsAsync((WorkspaceMember?)null);

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => CreateSut(request).ExecuteAsync());

        Assert.That(ex!.Message, Does.Contain("not member"));
    }
}
