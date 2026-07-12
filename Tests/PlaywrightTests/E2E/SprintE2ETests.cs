using Microsoft.Playwright;
using PlaywrightTests.Helpers;

namespace PlaywrightTests.E2E;

[Parallelizable(ParallelScope.Self)]
public class SprintE2ETests : E2eTestBase
{
    private ApiClient _client = null!;
    private TestEnvironment _env = null!;

    [SetUp]
    public async Task SetUp()
    {
        _client = await ApiClient.CreateAsync(Playwright);
        _env = await _client.CreateEnvironmentAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _client.DisposeAsync();
    }

    [Test]
    public async Task CreateSprint_ShowsSprintGroupInBacklog()
    {
        const string sprintName = "E2E Playwright Sprint";

        await E2eAppHelper.LoginAsync(Page, _env);
        await Page.GetByText("Add Sprint", new() { Exact = true }).ClickAsync();
        await E2eAppHelper.FillLabeledInputAsync(Page, "Sprint name", sprintName);
        await Page.Locator(".sprint-modal").GetByText("Create", new() { Exact = true }).ClickAsync();

        await Expect(Page.Locator(".sprint-header-text", new() { HasText = sprintName }))
            .ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    [Test]
    public async Task ViewSprint_ShowsExistingSprintInBacklog()
    {
        var sprintName = $"E2E Existing Sprint {Guid.NewGuid():N}";

        await _client.CreateSprintAsync(_env.BoardId, sprintName);

        await E2eAppHelper.LoginAsync(Page, _env);
        await E2eAppHelper.EnsureBacklogTabAsync(Page);

        await Expect(Page.Locator(".sprint-header-text", new() { HasText = sprintName }))
            .ToBeVisibleAsync(new() { Timeout = 15000 });
    }
}
