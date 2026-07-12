using Microsoft.Playwright;
using PlaywrightTests.Helpers;

namespace PlaywrightTests.E2E;

[Parallelizable(ParallelScope.Self)]
public class WorkspaceE2ETests : E2eTestBase
{
    [Test]
    public async Task CreateWorkspace_ShowsWorkspaceInSidebar()
    {
        await using var setup = await CreateApiClientAsync();
        await setup.AuthenticateAsync();

        const string workspaceName = "Design Team";

        await E2eAppHelper.LoginAsync(Page, setup.Email, setup.Password);
        await Page.GetByText("Create Workspace", new() { Exact = true }).ClickAsync();
        await Page.GetByLabel("Workspace name").FillAsync(workspaceName);

        var createResponse = await Page.RunAndWaitForResponseAsync(
            () => Page.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync(),
            response =>
                response.Url.Contains("/api/workspace", StringComparison.OrdinalIgnoreCase) &&
                response.Request.Method == "POST");

        Assert.That(createResponse.Ok, Is.True, "Workspace creation API call should succeed.");

        await Expect(Page.Locator(".workspace-name", new() { HasText = workspaceName }))
            .ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task ViewWorkspace_ShowsExistingWorkspaceName()
    {
        const string workspaceName = "Playwright Workspace";

        await using var setup = await CreateApiClientAsync();
        await setup.CreateEnvironmentAsync(workspaceName);

        await E2eAppHelper.LoginAsync(Page, setup.Email, setup.Password);

        await Expect(Page.Locator(".workspace-name", new() { HasText = workspaceName }))
            .ToBeVisibleAsync(new() { Timeout = 10000 });
    }
}
