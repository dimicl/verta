using PlaywrightTests.Helpers;

namespace PlaywrightTests.Api;

[FixtureLifeCycle(LifeCycle.SingleInstance)]
[NonParallelizable]
public class CommentApiTests : ApiTestBase
{
    private int _workItemId;
    private int _commentId;

    [OneTimeSetUp]
    public async Task OneTimeSetUpCommentTests()
    {
        var env = await Client.CreateEnvironmentAsync();
        _workItemId = await Client.CreateWorkItemAsync(env.BoardId);
    }

    [Test, Order(1)]
    public async Task CreateCommentReturnsOk()
    {
        _commentId = await Client.CreateCommentAsync(_workItemId, "API comment");
        Assert.That(_commentId, Is.GreaterThan(0));
    }

    [Test, Order(2)]
    public async Task GetCommentsReturnsOk()
    {
        Assert.That(_commentId, Is.GreaterThan(0));

        var response = await Client.GetCommentsByWorkItemAsync(_workItemId);

        Assert.That(response.Status, Is.EqualTo(200));
    }

    [Test, Order(3)]
    public async Task UpdateCommentReturnsOk()
    {
        Assert.That(_commentId, Is.GreaterThan(0));

        var response = await Client.UpdateCommentAsync(_commentId, "Updated API comment");

        Assert.That(response.Status, Is.EqualTo(200));
    }

    [Test, Order(4)]
    public async Task DeleteCommentReturnsNoContent()
    {
        Assert.That(_commentId, Is.GreaterThan(0));

        var response = await Client.DeleteCommentAsync(_commentId);

        Assert.That(response.Status, Is.EqualTo(204));
    }
}
