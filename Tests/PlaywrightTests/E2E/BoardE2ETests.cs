using Microsoft.Playwright;
using PlaywrightTests.Helpers;

namespace PlaywrightTests.E2E;

[Parallelizable(ParallelScope.Self)]
public class BoardE2ETests : E2eTestBase
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
    public async Task CreateBoard_ShowsBoardInSidebar()
    {
        const string boardName = "Design Board";

        await E2eAppHelper.LoginAsync(Page, _env);
        await Page.Locator(".add-board-btn").ClickAsync();
        await E2eAppHelper.FillLabeledInputAsync(Page, "Board name", boardName);
        await Page.Locator(".board-modal").GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();

        await Expect(Page.Locator(".board-item-name", new() { HasText = boardName }))
            .ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    [Test]
    public async Task SelectBoard_ShowsBoardTasksInBacklog()
    {
        const string boardName = "Product Board";
        const string taskName = "Board backlog task";

        var boardId = await _client.CreateBoardAsync(_env.WorkspaceId, boardName);
        await _client.CreateWorkItemAsync(boardId, taskName, "Task on secondary board");

        await E2eAppHelper.LoginAsync(Page, _env);
        await Page.Locator(".board-item-name", new() { HasText = boardName }).ClickAsync();
        await E2eAppHelper.EnsureBacklogTabAsync(Page);

        await Expect(Page.GetByText(taskName)).ToBeVisibleAsync(new() { Timeout = 15000 });
    }
}
