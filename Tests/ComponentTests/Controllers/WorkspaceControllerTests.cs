using backend.Application.Exceptions;
using backend.Application.Interfaces;
using ComponentTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ComponentTests.Controllers;

[TestFixture]
public class WorkspaceControllerTests
{
    private Mock<IWorkspaceService> _serviceMock = null!;
    private Mock<IWorkspaceMemberService> _memberServiceMock = null!;
    private Mock<IInvitationService> _invitationServiceMock = null!;
    private WorkspaceController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IWorkspaceService>();
        _memberServiceMock = new Mock<IWorkspaceMemberService>();
        _invitationServiceMock = new Mock<IInvitationService>();
        _controller = new WorkspaceController(
            _serviceMock.Object,
            _memberServiceMock.Object,
            _invitationServiceMock.Object);
    }

  #region Create

    [Test]
    public async Task CreateValidRequestReturnsOk()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        var response = TestDataFactory.CreateWorkspaceResponse();
        _serviceMock.Setup(s => s.Create(request)).ReturnsAsync(response);

        var result = await _controller.CreateWorkspace(request);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(response));
    }

    [Test]
    public void CreateAlreadyHasWorkspacePropagatesValidation()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new ValidationException("User already has a workspace."));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _controller.CreateWorkspace(request));

        Assert.That(ex!.Message, Does.Contain("already has"));
    }

    [Test]
    public void CreateMissingNamePropagatesValidation()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new ValidationException("Workspace name is required."));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _controller.CreateWorkspace(request));

        Assert.That(ex!.Message, Does.Contain("required"));
    }

  #endregion

  #region Read

    [Test]
    public async Task GetMyWorkspaceExistingReturnsOk()
    {
        var response = TestDataFactory.CreateWorkspaceResponse();
        _serviceMock.Setup(s => s.GetByOwnerId()).ReturnsAsync(response);

        var result = await _controller.GetMyWorkspace();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(response));
    }

    [Test]
    public void GetMyWorkspaceNotFoundPropagatesNotFound()
    {
        _serviceMock.Setup(s => s.GetByOwnerId())
            .ThrowsAsync(new NotFoundException("Workspace does not exist."));

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _controller.GetMyWorkspace());

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void GetMyWorkspaceUnauthorizedPropagatesForbidden()
    {
        _serviceMock.Setup(s => s.GetByOwnerId())
            .ThrowsAsync(new ForbiddenException("User is not authenticated."));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _controller.GetMyWorkspace());

        Assert.That(ex!.Message, Does.Contain("not authenticated"));
    }

  #endregion

  #region Update

    [Test]
    public async Task UpdateValidRequestReturnsOk()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        var response = TestDataFactory.CreateWorkspaceResponse();
        _serviceMock.Setup(s => s.Update(1, request)).ReturnsAsync(response);

        var result = await _controller.UpdateWorkspace(1, request);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
    }

    [Test]
    public void UpdateNotFoundPropagatesNotFound()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        _serviceMock.Setup(s => s.Update(999, request))
            .ThrowsAsync(new NotFoundException("Workspace does not exist."));

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _controller.UpdateWorkspace(999, request));

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void UpdateNotOwnerPropagatesForbidden()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        _serviceMock.Setup(s => s.Update(1, request))
            .ThrowsAsync(new ForbiddenException("You are not the owner of this workspace."));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _controller.UpdateWorkspace(1, request));

        Assert.That(ex!.Message, Does.Contain("not the owner"));
    }

  #endregion

  #region Delete

    [Test]
    public async Task DeleteExistingIdReturnsNoContent()
    {
        _serviceMock.Setup(s => s.Delete(1)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteWorkspace(1);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public void DeleteNotFoundPropagatesNotFound()
    {
        _serviceMock.Setup(s => s.Delete(999))
            .ThrowsAsync(new NotFoundException("Workspace does not exist."));

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _controller.DeleteWorkspace(999));

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void DeleteNotOwnerPropagatesForbidden()
    {
        _serviceMock.Setup(s => s.Delete(1))
            .ThrowsAsync(new ForbiddenException("You are not the owner of this workspace."));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _controller.DeleteWorkspace(1));

        Assert.That(ex!.Message, Does.Contain("not the owner"));
    }

  #endregion
}
