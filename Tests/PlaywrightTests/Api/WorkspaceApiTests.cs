using PlaywrightTests.Helpers;

namespace PlaywrightTests.Api;

[FixtureLifeCycle(LifeCycle.SingleInstance)]
[NonParallelizable]
public class WorkspaceApiTests : ApiTestBase
{
    private int _workspaceId;

    [Test, Order(1)]
    public async Task CreateWorkspaceTest()
    {
        await Client.AuthenticateAsync();
        _workspaceId = await Client.CreateWorkspaceAsync("Test Workspace");
        Assert.That(_workspaceId, Is.GreaterThan(0));
    }

    [Test, Order(2)]
    public async Task GetMyWorkspaceTest()
    {
        Assert.That(_workspaceId, Is.GreaterThan(0));

        var response = await Client.GetMyWorkspaceAsync();

        Assert.That(response.Status, Is.EqualTo(200));
    }

    [Test, Order(3)]
    public async Task UpdateWorkspaceTest()
    {
        Assert.That(_workspaceId, Is.GreaterThan(0));

        var response = await Client.UpdateWorkspaceAsync(_workspaceId, "Updated Workspace");

        Assert.That(response.Status, Is.EqualTo(200));
    }

    [Test, Order(4)]
    public async Task DeleteWorkspaceNoContent()
    {
        Assert.That(_workspaceId, Is.GreaterThan(0));

        var response = await Client.DeleteWorkspaceAsync(_workspaceId);

        Assert.That(response.Status, Is.EqualTo(204));
    }
}
