using Microsoft.Playwright;
using PlaywrightTests.Helpers;

namespace PlaywrightTests.E2E;

[Parallelizable(ParallelScope.Self)]
public class CommentE2ETests : E2eTestBase
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
    public async Task AddComment_ShowsCommentInTaskModal()
    {
        var taskName = $"E2E Comment Task {Guid.NewGuid():N}";
        const string commentText = "Projekat je super!";

        await _client.CreateWorkItemAsync(_env.BoardId, taskName, "Task for comment E2E");

        await E2eAppHelper.LoginAsync(Page, _env);
        await E2eAppHelper.OpenTaskModalAsync(Page, taskName);
        await E2eAppHelper.AddCommentInOpenTaskAsync(Page, commentText);
    }

    [Test]
    public async Task EditComment_UpdatesVisibleContent()
    {
        var taskName = $"E2E Edit Comment Task {Guid.NewGuid():N}";
        const string originalText = "Originalni komentar";
        const string updatedText = "Izmenjen komentar";

        await _client.CreateWorkItemAsync(_env.BoardId, taskName, "Task for edit comment");

        await E2eAppHelper.LoginAsync(Page, _env);
        await E2eAppHelper.OpenTaskModalAsync(Page, taskName);
        await E2eAppHelper.AddCommentInOpenTaskAsync(Page, originalText);
        await E2eAppHelper.EditCommentInOpenTaskAsync(Page, originalText, updatedText);
    }

    [Test]
    public async Task DeleteComment_RemovesCommentFromTaskModal()
    {
        var taskName = $"E2E Delete Comment Task {Guid.NewGuid():N}";
        const string commentText = "Komentar za brisanje";

        var workItemId = await _client.CreateWorkItemAsync(
            _env.BoardId,
            taskName,
            "Task for delete comment");
        await _client.CreateCommentAsync(workItemId, commentText);

        await E2eAppHelper.LoginAsync(Page, _env);
        await E2eAppHelper.OpenTaskModalAsync(Page, taskName);
        await E2eAppHelper.DeleteCommentInOpenTaskAsync(Page, commentText);
    }
}
