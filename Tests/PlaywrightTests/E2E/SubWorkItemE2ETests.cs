using Microsoft.Playwright;
using PlaywrightTests.Helpers;

namespace PlaywrightTests.E2E;

[Parallelizable(ParallelScope.Self)]
public class SubWorkItemE2ETests : E2eTestBase
{
    private ApiClient _client = null!;
    private TestEnvironment _env = null!;
    private string _taskName = null!;

    [SetUp]
    public async Task SetUp()
    {
        _client = await ApiClient.CreateAsync(Playwright);
        _env = await _client.CreateEnvironmentAsync();
        _taskName = $"E2E Subtask Parent {Guid.NewGuid():N}";
        await _client.CreateWorkItemAsync(_env.BoardId, _taskName, "Parent task for subtask E2E");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _client.DisposeAsync();
    }

    [Test]
    public async Task CreateSubtask_ShowsSubtaskInTaskModal()
    {
        const string subtaskTitle = "E2E Subtask Create";
        const string subtaskDescription = "Subtask created from Playwright";

        await E2eAppHelper.LoginAsync(Page, _env);
        await E2eAppHelper.OpenTaskModalAsync(Page, _taskName);
        await E2eAppHelper.CreateSubtaskInOpenTaskAsync(Page, subtaskTitle, subtaskDescription);

        await Expect(Page.Locator(".task-subtasks__description", new() { HasText = subtaskDescription }))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task UpdateSubtask_ChangesSubtaskTitleInList()
    {
        const string subtaskTitle = "E2E Subtask Before";
        const string updatedTitle = "E2E Subtask After";

        await E2eAppHelper.LoginAsync(Page, _env);
        await E2eAppHelper.OpenTaskModalAsync(Page, _taskName);
        await E2eAppHelper.CreateSubtaskInOpenTaskAsync(
            Page,
            subtaskTitle,
            "Initial subtask description");
        await E2eAppHelper.UpdateSubtaskInOpenTaskAsync(Page, subtaskTitle, updatedTitle);
    }
}
