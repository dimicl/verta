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
    public async Task CreateAlreadyHasWorkspaceReturnsBadRequest()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new Exception("User already has a workspace."));

        var result = await _controller.CreateWorkspace(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateMissingNameReturnsBadRequest()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new Exception("Workspace name is required."));

        var result = await _controller.CreateWorkspace(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
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
    public async Task GetMyWorkspaceNotFoundReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.GetByOwnerId())
            .ThrowsAsync(new Exception("Workspace does not exist."));

        var result = await _controller.GetMyWorkspace();

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetMyWorkspaceUnauthorizedReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.GetByOwnerId())
            .ThrowsAsync(new Exception("User is not authenticated."));

        var result = await _controller.GetMyWorkspace();

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
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
    public async Task UpdateNotFoundReturnsBadRequest()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        _serviceMock.Setup(s => s.Update(999, request))
            .ThrowsAsync(new Exception("Workspace does not exist."));

        var result = await _controller.UpdateWorkspace(999, request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateNotOwnerReturnsBadRequest()
    {
        var request = TestDataFactory.CreateWorkspaceRequest();
        _serviceMock.Setup(s => s.Update(1, request))
            .ThrowsAsync(new Exception("You are not the owner of this workspace."));

        var result = await _controller.UpdateWorkspace(1, request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
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
    public async Task DeleteNotFoundReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.Delete(999))
            .ThrowsAsync(new Exception("Workspace does not exist."));

        var result = await _controller.DeleteWorkspace(999);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DeleteNotOwnerReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.Delete(1))
            .ThrowsAsync(new Exception("You are not the owner of this workspace."));

        var result = await _controller.DeleteWorkspace(1);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

  #endregion
}
