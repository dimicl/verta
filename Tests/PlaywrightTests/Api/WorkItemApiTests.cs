using PlaywrightTests.Helpers;

namespace PlaywrightTests.Api;

[FixtureLifeCycle(LifeCycle.SingleInstance)]
[NonParallelizable]
public class WorkItemApiTests : ApiTestBase
{
    private int _boardId;
    private int _workItemId;

    [OneTimeSetUp]
    public async Task OneTimeSetUpWorkItemTests()
    {
        var env = await Client.CreateEnvironmentAsync();
        _boardId = env.BoardId;
    }

    [Test, Order(1)]
    public async Task CreateWorkItemReturnsOk()
    {
        _workItemId = await Client.CreateWorkItemAsync(_boardId, "API Work Item", "API description");
        Assert.That(_workItemId, Is.GreaterThan(0));
    }

    [Test, Order(2)]
    public async Task GetWorkItemReturnsOk()
    {
        Assert.That(_workItemId, Is.GreaterThan(0));

        var response = await Client.GetWorkItemAsync(_workItemId);

        Assert.That(response.Status, Is.EqualTo(200));
    }

    [Test, Order(3)]
    public async Task UpdateWorkItemReturnsOk()
    {
        Assert.That(_workItemId, Is.GreaterThan(0));

        var response = await Client.UpdateWorkItemAsync(
            _workItemId,
            _boardId,
            "Updated API Work Item",
            "Updated description");

        Assert.That(response.Status, Is.EqualTo(200));
    }

    [Test, Order(4)]
    public async Task DeleteWorkItemReturnsNoContent()
    {
        Assert.That(_workItemId, Is.GreaterThan(0));

        var response = await Client.DeleteWorkItemAsync(_workItemId);

        Assert.That(response.Status, Is.EqualTo(204));
    }
}
