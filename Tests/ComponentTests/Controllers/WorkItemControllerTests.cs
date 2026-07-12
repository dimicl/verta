using backend.Application.Interfaces;
using ComponentTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ComponentTests.Controllers;

[TestFixture]
public class WorkItemControllerTests
{
    private Mock<IWorkItemService> _serviceMock = null!;
    private WorkItemController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IWorkItemService>();
        _controller = new WorkItemController(_serviceMock.Object);
    }

  #region Create

    [Test]
    public async Task CreateValidRequestReturnsOk()
    {
        var request = TestDataFactory.CreateWorkItemRequest();
        var response = TestDataFactory.CreateWorkItemResponse();
        _serviceMock.Setup(s => s.Create(request)).ReturnsAsync(response);

        var result = await _controller.Create(request);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(response));
    }

    [Test]
    public async Task CreateInvalidBoardReturnsBadRequest()
    {
        var request = TestDataFactory.CreateWorkItemRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new Exception("Board does not exist."));

        var result = await _controller.Create(request);

        var badRequest = result as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
    }

    [Test]
    public async Task CreateMissingNameReturnsBadRequest()
    {
        var request = TestDataFactory.CreateWorkItemRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new Exception("Work item name is required."));

        var result = await _controller.Create(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

  #endregion

  #region Read

    [Test]
    public async Task GetByIdExistingIdReturnsOk()
    {
        var response = TestDataFactory.CreateWorkItemResponse();
        _serviceMock.Setup(s => s.GetById(1)).ReturnsAsync(response);

        var result = await _controller.GetById(1);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(response));
    }

    [Test]
    public async Task GetByBoardIdReturnsOrderedWorkItemsForBoard()
    {
        var items = TestDataFactory.CreateWorkItemList(1, 2, 3);
        _serviceMock.Setup(s => s.GetByBoardId(1)).ReturnsAsync(items);

        var result = await _controller.GetByBoardId(1);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var actual = ((OkObjectResult)result).Value as List<WorkItemResponse>;

        Assert.That(actual, Is.Not.Null);
        CollectionAssert.AllItemsAreInstancesOfType(actual!, typeof(WorkItemResponse));
        CollectionAssert.IsOrdered(actual!.Select(i => i.Id).ToList());
        Assert.That(actual.Select(i => i.BoardId), Is.All.EqualTo(1));
        Assert.That(actual.Select(i => i.Name), Has.All.Not.Empty);
    }

    [Test]
    public async Task GetByBoardIdEmptyBoardReturnsEmptyCollection()
    {
        _serviceMock.Setup(s => s.GetByBoardId(1))
            .ReturnsAsync(new List<WorkItemResponse>());

        var result = await _controller.GetByBoardId(1);
        var actual = ((OkObjectResult)result).Value as List<WorkItemResponse>;

        Assert.That(actual, Is.Not.Null);
        CollectionAssert.IsEmpty(actual!);
    }

    [Test]
    public async Task GetByIdNotFoundReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.GetById(999))
            .ThrowsAsync(new Exception("Work item does not exist."));

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
        var request = TestDataFactory.CreateWorkItemRequest();
        var response = TestDataFactory.CreateWorkItemResponse();
        _serviceMock.Setup(s => s.Update(1, request)).ReturnsAsync(response);

        var result = await _controller.Update(1, request);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(response));
    }

    [Test]
    public async Task UpdateNotFoundReturnsBadRequest()
    {
        var request = TestDataFactory.CreateWorkItemRequest();
        _serviceMock.Setup(s => s.Update(999, request))
            .ThrowsAsync(new Exception("Work item does not exist."));

        var result = await _controller.Update(999, request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateWithoutWriteLockReturnsBadRequest()
    {
        var request = TestDataFactory.CreateWorkItemRequest();
        _serviceMock.Setup(s => s.Update(1, request))
            .ThrowsAsync(new Exception("This work item is locked by another user."));

        var result = await _controller.Update(1, request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result;
        Assert.That(badRequest.Value, Is.Not.Null);
    }

    [Test]
    public async Task UpdateInvalidNameReturnsBadRequest()
    {
        var request = TestDataFactory.CreateWorkItemRequest();
        _serviceMock.Setup(s => s.Update(1, request))
            .ThrowsAsync(new Exception("Work item name is required."));

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
            .ThrowsAsync(new Exception("Work item does not exist."));

        var result = await _controller.Delete(999);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DeleteNoWriteLockReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.Delete(1))
            .ThrowsAsync(new Exception("You do not have write access to this work item."));

        var result = await _controller.Delete(1);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

  #endregion
}
