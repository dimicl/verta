using backend.Application.Exceptions;
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
    public void CreateMissingNamePropagatesValidation()
    {
        var request = TestDataFactory.CreateSprintRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new ValidationException("Sprint name is required."));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _controller.Create(request));

        Assert.That(ex!.Message, Does.Contain("required"));
    }

    [Test]
    public void CreateBoardNotFoundPropagatesNotFound()
    {
        var request = TestDataFactory.CreateSprintRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new NotFoundException("Board does not exist."));

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _controller.Create(request));

        Assert.That(ex!.Message, Does.Contain("does not exist"));
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
    public void GetByIdNotFoundPropagatesNotFound()
    {
        _serviceMock.Setup(s => s.GetById(999))
            .ThrowsAsync(new NotFoundException("Sprint does not exist."));

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _controller.GetById(999));

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void GetByIdNoAccessPropagatesForbidden()
    {
        _serviceMock.Setup(s => s.GetById(1))
            .ThrowsAsync(new ForbiddenException("You do not have access to this board."));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _controller.GetById(1));

        Assert.That(ex!.Message, Does.Contain("do not have access"));
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
    public void UpdateNotFoundPropagatesNotFound()
    {
        var request = TestDataFactory.CreateUpdateSprintRequest();
        _serviceMock.Setup(s => s.Update(999, request))
            .ThrowsAsync(new NotFoundException("Sprint does not exist."));

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _controller.Update(999, request));

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void UpdateInvalidDatesPropagatesValidation()
    {
        var request = TestDataFactory.CreateUpdateSprintRequest();
        _serviceMock.Setup(s => s.Update(1, request))
            .ThrowsAsync(new ValidationException("End date must be on or after start date."));

        var ex = Assert.ThrowsAsync<ValidationException>(() => _controller.Update(1, request));

        Assert.That(ex!.Message, Does.Contain("End date"));
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
    public void DeleteNotFoundPropagatesNotFound()
    {
        _serviceMock.Setup(s => s.Delete(999))
            .ThrowsAsync(new NotFoundException("Sprint does not exist."));

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _controller.Delete(999));

        Assert.That(ex!.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void DeleteNoAccessPropagatesForbidden()
    {
        _serviceMock.Setup(s => s.Delete(1))
            .ThrowsAsync(new ForbiddenException("You do not have access to this board."));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _controller.Delete(1));

        Assert.That(ex!.Message, Does.Contain("do not have access"));
    }

  #endregion
}
