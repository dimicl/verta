using backend.Application.Interfaces;
using ComponentTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ComponentTests.Controllers;

[TestFixture]
public class SprintControllerTests
{
    private Mock<ISprintService> _serviceMock = null!;
    private SprintController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<ISprintService>();
        _controller = new SprintController(_serviceMock.Object);
    }

  #region Create

    [Test]
    public async Task CreateValidRequestReturnsOk()
    {
        var request = TestDataFactory.CreateSprintRequest();
        var response = TestDataFactory.CreateSprintResponse();
        _serviceMock.Setup(s => s.Create(request)).ReturnsAsync(response);

        var result = await _controller.Create(request);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(response));
    }

    [Test]
    public async Task CreateMissingNameReturnsBadRequest()
    {
        var request = TestDataFactory.CreateSprintRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new Exception("Sprint name is required."));

        var result = await _controller.Create(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateBoardNotFoundReturnsBadRequest()
    {
        var request = TestDataFactory.CreateSprintRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new Exception("Board does not exist."));

        var result = await _controller.Create(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

  #endregion

  #region Read

    [Test]
    public async Task GetByIdExistingIdReturnsOk()
    {
        var response = TestDataFactory.CreateSprintResponse();
        _serviceMock.Setup(s => s.GetById(1)).ReturnsAsync(response);

        var result = await _controller.GetById(1);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(response));
    }

    [Test]
    public async Task GetByIdNotFoundReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.GetById(999))
            .ThrowsAsync(new Exception("Sprint does not exist."));

        var result = await _controller.GetById(999);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetByIdNoAccessReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.GetById(1))
            .ThrowsAsync(new Exception("You do not have access to this board."));

        var result = await _controller.GetById(1);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

  #endregion

  #region Update

    [Test]
    public async Task UpdateValidRequestReturnsOk()
    {
        var request = TestDataFactory.CreateUpdateSprintRequest();
        var response = TestDataFactory.CreateSprintResponse();
        _serviceMock.Setup(s => s.Update(1, request)).ReturnsAsync(response);

        var result = await _controller.Update(1, request);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
    }

    [Test]
    public async Task UpdateNotFoundReturnsBadRequest()
    {
        var request = TestDataFactory.CreateUpdateSprintRequest();
        _serviceMock.Setup(s => s.Update(999, request))
            .ThrowsAsync(new Exception("Sprint does not exist."));

        var result = await _controller.Update(999, request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateInvalidDatesReturnsBadRequest()
    {
        var request = TestDataFactory.CreateUpdateSprintRequest();
        _serviceMock.Setup(s => s.Update(1, request))
            .ThrowsAsync(new Exception("End date must be on or after start date."));

        var result = await _controller.Update(1, request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

  #endregion

  #region Delete

    [Test]
    public async Task DeleteExistingIdReturnsNoContent()
    {
        _serviceMock.Setup(s => s.Delete(1)).Returns(Task.CompletedTask);

        var result = await _controller.Delete(1);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteNotFoundReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.Delete(999))
            .ThrowsAsync(new Exception("Sprint does not exist."));

        var result = await _controller.Delete(999);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DeleteNoAccessReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.Delete(1))
            .ThrowsAsync(new Exception("You do not have access to this board."));

        var result = await _controller.Delete(1);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

  #endregion
}
