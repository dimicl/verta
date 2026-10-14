using backend.Application.Exceptions;
using backend.Application.Interfaces;
using backend.Application.Services;
using ComponentTests.Helpers;
using Moq;

namespace ComponentTests.Services;

[TestFixture]
public class WorkspaceServiceTests
{
    private Mock<IWorkspaceRepository> _repo = null!;
    private Mock<IWorkspaceMemberRepository> _memberRepo = null!;
    private Mock<IUserContext> _user = null!;
    private WorkspaceService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IWorkspaceRepository>();
        _memberRepo = new Mock<IWorkspaceMemberRepository>();
        _user = new Mock<IUserContext>();
        _user.Setup(u => u.GetUserId()).Returns(1);

        _sut = new WorkspaceService(_repo.Object, _memberRepo.Object, _user.Object);
    }

    [Test]
    public async Task Create_FirstWorkspace_AddsWorkspaceAndOwnerMember()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        _repo.Setup(r => r.GetByOwnerIdAsync(1)).ReturnsAsync((Workspace?)null);
        _repo.Setup(r => r.Add(It.IsAny<Workspace>()))
            .ReturnsAsync((Workspace w) =>
            {
                w.Id = 8;
                return w;
            });

        var result = await _sut.Create(request);

        Assert.That(result.Id, Is.EqualTo(8));
        Assert.That(result.Name, Is.EqualTo("Demo Workspace"));
        Assert.That(result.OwnerId, Is.EqualTo(1));
        _memberRepo.Verify(r => r.Add(It.Is<WorkspaceMember>(
            m => m.WorkspaceId == 8 && m.UserId == 1 && m.Role == UserRole.Owner)), Times.Once);
    }

    [Test]
    public void Create_WhenUserAlreadyHasWorkspace_ThrowsValidation()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        _repo.Setup(r => r.GetByOwnerIdAsync(1))
            .ReturnsAsync(TestDataFactory.CreateWorkspace());

        var ex = Assert.ThrowsAsync<ValidationException>(() => _sut.Create(request));

        Assert.That(ex!.Message, Does.Contain("already has"));
        _repo.Verify(r => r.Add(It.IsAny<Workspace>()), Times.Never);
    }

    [Test]
    public async Task GetByOwnerId_OwnedWorkspace_ReturnsIt()
    {
        _memberRepo.Setup(r => r.GetByUserIdAsync(1))
            .ReturnsAsync(new List<WorkspaceMember>
            {
                TestDataFactory.CreateMember(role: UserRole.Owner)
            });
        _repo.Setup(r => r.GetByOwnerIdAsync(1))
            .ReturnsAsync(TestDataFactory.CreateWorkspace());

        var result = await _sut.GetByOwnerId();

        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.OwnerId, Is.EqualTo(1));
    }

    [Test]
    public void GetByOwnerId_Missing_ThrowsNotFound()
    {
        _memberRepo.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(new List<WorkspaceMember>());
        _repo.Setup(r => r.GetByOwnerIdAsync(1)).ReturnsAsync((Workspace?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByOwnerId());

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public async Task Update_AsOwner_UpdatesName()
    {
        var request = TestDataFactory.CreateWorkspaceRequest("Renamed");
        var workspace = TestDataFactory.CreateWorkspace();
        _repo.Setup(r => r.GetById(1)).ReturnsAsync(workspace);

        var result = await _sut.Update(1, request);

        Assert.That(result.Name, Is.EqualTo("Renamed"));
        _repo.Verify(r => r.Update(workspace), Times.Once);
    }

    [Test]
    public void Update_Missing_ThrowsNotFound()
    {
        _repo.Setup(r => r.GetById(999)).ReturnsAsync((Workspace?)null);

        Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Update(999, TestDataFactory.CreateWorkspaceRequest()));
    }

    [Test]
    public void Update_NotOwner_ThrowsForbidden()
    {
        _repo.Setup(r => r.GetById(1))
            .ReturnsAsync(TestDataFactory.CreateWorkspace(ownerId: 99));

        var ex = Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.Update(1, TestDataFactory.CreateWorkspaceRequest()));

        Assert.That(ex!.Message, Does.Contain("not the owner"));
    }

    [Test]
    public void Update_EmptyName_ThrowsValidation()
    {
        _repo.Setup(r => r.GetById(1)).ReturnsAsync(TestDataFactory.CreateWorkspace());

        var ex = Assert.ThrowsAsync<ValidationException>(
            () => _sut.Update(1, TestDataFactory.CreateWorkspaceRequest("  ")));

        Assert.That(ex!.Message, Does.Contain("required"));
    }

    [Test]
    public async Task Delete_AsOwner_DeletesWorkspace()
    {
        var workspace = TestDataFactory.CreateWorkspace();
        _repo.Setup(r => r.GetById(1)).ReturnsAsync(workspace);

        await _sut.Delete(1);

        _repo.Verify(r => r.Delete(workspace), Times.Once);
    }

    [Test]
    public void Delete_Missing_ThrowsNotFound()
    {
        _repo.Setup(r => r.GetById(999)).ReturnsAsync((Workspace?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.Delete(999));
    }

    [Test]
    public void Delete_NotOwner_ThrowsForbidden()
    {
        _repo.Setup(r => r.GetById(1))
            .ReturnsAsync(TestDataFactory.CreateWorkspace(ownerId: 99));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _sut.Delete(1));

        Assert.That(ex!.Message, Does.Contain("not the owner"));
    }
}
