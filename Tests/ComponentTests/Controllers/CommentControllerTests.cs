using backend.Application.Interfaces;
using ComponentTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ComponentTests.Controllers;

[TestFixture]
public class CommentControllerTests
{
    private Mock<ICommentService> _serviceMock = null!;
    private CommentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<ICommentService>();
        _controller = new CommentController(_serviceMock.Object);
    }

  #region Create

    [Test]
    public async Task CreateValidRequestReturnsOk()
    {
        var request = TestDataFactory.CreateCommentRequest();
        var response = TestDataFactory.CreateCommentResponse();
        _serviceMock.Setup(s => s.Create(request)).ReturnsAsync(response);

        var result = await _controller.Create(request);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(response));
    }

    [Test]
    public async Task CreateEmptyContentReturnsBadRequest()
    {
        var request = TestDataFactory.CreateCommentRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new Exception("Comment content is required."));

        var result = await _controller.Create(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateWorkItemNotFoundReturnsBadRequest()
    {
        var request = TestDataFactory.CreateCommentRequest();
        _serviceMock.Setup(s => s.Create(request))
            .ThrowsAsync(new Exception("Work item does not exist."));

        var result = await _controller.Create(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

  #endregion

  #region Read

    [Test]
    public async Task GetByWorkItemIdExistingCommentsReturnsOk()
    {
        var comments = TestDataFactory.CreateCommentList(1, 2, 3);
        _serviceMock.Setup(s => s.GetByWorkItemId(1)).ReturnsAsync(comments);

        var result = await _controller.GetByWorkItemId(1);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var actual = ((OkObjectResult)result).Value as List<CommentResponse>;

        Assert.That(actual, Has.Count.EqualTo(3));
        CollectionAssert.AllItemsAreInstancesOfType(actual!, typeof(CommentResponse));
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, actual!.Select(c => c.Id).ToArray());
        Assert.That(actual.Select(c => c.WorkItemId), Is.All.EqualTo(1));
        Assert.That(actual.Select(c => c.Content), Has.All.Not.Empty);
    }

    [Test]
    public async Task GetByWorkItemIdWorkItemNotFoundReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.GetByWorkItemId(999))
            .ThrowsAsync(new Exception("Work item does not exist."));

        var result = await _controller.GetByWorkItemId(999);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetByWorkItemIdEmptyListReturnsOk()
    {
        _serviceMock.Setup(s => s.GetByWorkItemId(1))
            .ReturnsAsync(new List<CommentResponse>());

        var result = await _controller.GetByWorkItemId(1);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var actual = ((OkObjectResult)result).Value as List<CommentResponse>;
        CollectionAssert.IsEmpty(actual!);
    }

  #endregion

  #region Update

    [Test]
    public async Task UpdateValidRequestReturnsOk()
    {
        var request = TestDataFactory.CreateUpdateCommentRequest();
        var response = TestDataFactory.CreateCommentResponse();
        response = response with { Content = request.Content };
        _serviceMock.Setup(s => s.Update(1, request)).ReturnsAsync(response);

        var result = await _controller.Update(1, request);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
    }

    [Test]
    public async Task UpdateNotFoundReturnsBadRequest()
    {
        var request = TestDataFactory.CreateUpdateCommentRequest();
        _serviceMock.Setup(s => s.Update(999, request))
            .ThrowsAsync(new Exception("Comment does not exist."));

        var result = await _controller.Update(999, request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateNotOwnerReturnsBadRequest()
    {
        var request = TestDataFactory.CreateUpdateCommentRequest();
        _serviceMock.Setup(s => s.Update(1, request))
            .ThrowsAsync(new Exception("You can only edit your own comments."));

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
            .ThrowsAsync(new Exception("Comment does not exist."));

        var result = await _controller.Delete(999);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DeleteNotOwnerReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.Delete(1))
            .ThrowsAsync(new Exception("You can only delete your own comments."));

        var result = await _controller.Delete(1);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

  #endregion
}
