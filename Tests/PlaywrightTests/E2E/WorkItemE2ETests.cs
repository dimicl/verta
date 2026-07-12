using Microsoft.Playwright;
using PlaywrightTests.Helpers;

namespace PlaywrightTests.E2E;

[Parallelizable(ParallelScope.Self)]
public class WorkItemE2ETests : E2eTestBase
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
    public async Task CreateWorkItem_FromUi_ShowsTaskInBacklog()
    {
        const string taskName = "E2E Playwright Task";
        const string description = "Created from Playwright E2E test";

        await E2eAppHelper.LoginAsync(Page, _env);
        await E2eAppHelper.CreateTaskFromUiAsync(Page, taskName, description);

        await Expect(Page.Locator(".task-description", new() { HasText = description }))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task ViewWorkItem_OpensModalWithTaskDetails()
    {
        var taskName = $"E2E View Task {Guid.NewGuid():N}";
        const string description = "Task prepared for view E2E";

        await _client.CreateWorkItemAsync(_env.BoardId, taskName, description);

        await E2eAppHelper.LoginAsync(Page, _env);
        await E2eAppHelper.OpenTaskModalAsync(Page, taskName);

        await Expect(Page.Locator(".title-input")).ToHaveValueAsync(taskName);
        await Expect(Page.Locator(".description-textarea")).ToHaveValueAsync(description);
    }

    [Test]
    public async Task UpdateWorkItem_ChangesTitleInBacklog()
    {
        var taskName = $"E2E Update Task {Guid.NewGuid():N}";
        var updatedName = $"{taskName} Updated";

        await _client.CreateWorkItemAsync(_env.BoardId, taskName, "Before update");

        await E2eAppHelper.LoginAsync(Page, _env);
        await E2eAppHelper.OpenTaskModalAsync(Page, taskName);
        await E2eAppHelper.UpdateOpenTaskAsync(Page, updatedName, "After update");

        await Expect(Page.Locator(".task-title", new() { HasText = updatedName })).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    [Test]
    public async Task DeleteWorkItem_RemovesTaskFromBacklog()
    {
        var taskName = $"E2E Delete Task {Guid.NewGuid():N}";
        var workItemId = await _client.CreateWorkItemAsync(_env.BoardId, taskName, "Task to delete");

        await E2eAppHelper.LoginAsync(Page, _env);
        await Expect(Page.Locator(".task-title", new() { HasText = taskName })).ToBeVisibleAsync(new() { Timeout = 15000 });
        await E2eAppHelper.DeleteTaskAsync(Page, _client, workItemId, taskName);
    }
}
