using PlaywrightTests.Helpers;

namespace PlaywrightTests.Api;

[FixtureLifeCycle(LifeCycle.SingleInstance)]
[NonParallelizable]
public class SprintApiTests : ApiTestBase
{
    private int _boardId;
    private int _sprintId;

    [OneTimeSetUp]
    public async Task OneTimeSetUpSprintTests()
    {
        var env = await Client.CreateEnvironmentAsync();
        _boardId = env.BoardId;
    }

    [Test, Order(1)]
    public async Task CreateSprintReturnsOk()
    {
        _sprintId = await Client.CreateSprintAsync(_boardId);
        Assert.That(_sprintId, Is.GreaterThan(0));
    }

    [Test, Order(2)]
    public async Task GetSprintReturnsOk()
    {
        Assert.That(_sprintId, Is.GreaterThan(0));

        var response = await Client.GetSprintAsync(_sprintId);

        Assert.That(response.Status, Is.EqualTo(200));
    }

    [Test, Order(3)]
    public async Task UpdateSprintReturnsOk()
    {
        Assert.That(_sprintId, Is.GreaterThan(0));

        var response = await Client.UpdateSprintAsync(_sprintId, "Updated Playwright Sprint");

        Assert.That(response.Status, Is.EqualTo(200));
    }

    [Test, Order(4)]
    public async Task DeleteSprintReturnsNoContent()
    {
        Assert.That(_sprintId, Is.GreaterThan(0));

        var response = await Client.DeleteSprintAsync(_sprintId);

        Assert.That(response.Status, Is.EqualTo(204));
    }
}
