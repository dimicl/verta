using Microsoft.Playwright;
using PlaywrightTests.Configuration;

namespace PlaywrightTests.Helpers;

public sealed class ApiClient : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private IAPIRequestContext _request;

    public string? Token { get; private set; }
    public string Email { get; }
    public string Password { get; }

    private ApiClient(IPlaywright playwright, IAPIRequestContext request, string? email, string? password)
    {
        _playwright = playwright;
        _request = request;
        Email = email ?? $"playwright_{Guid.NewGuid():N}@test.local";
        Password = password ?? TestSettings.TestPassword;
    }

    public static async Task<ApiClient> CreateAsync(IPlaywright playwright, string? email = null, string? password = null)
    {
        var request = await NewRequestContextAsync(playwright);
        return new ApiClient(playwright, request, email, password);
    }

    public async Task AuthenticateAsync()
    {
        var registerResponse = await _request.PostAsync("register", new()
        {
            DataObject = new
            {
                firstName = "Test",
                lastName = "Test",
                email = Email,
                password = Password
            }
        });

        if (!registerResponse.Ok)
        {
            var loginResponse = await _request.PostAsync("login", new()
            {
                DataObject = new { email = Email, password = Password }
            });
            await EnsureSuccessAsync(loginResponse);
            Token = await ExtractTokenAsync(loginResponse);
        }
        else
        {
            Token = await ExtractTokenAsync(registerResponse);
        }

        await _request.DisposeAsync();
        _request = await NewRequestContextAsync(_playwright, Token);
    }

    public async Task<int> CreateWorkspaceAsync(string name = "Playwright Workspace")
    {
        var response = await _request.PostAsync("workspace", new() { DataObject = new { name } });
        await EnsureSuccessAsync(response);
        return (await ParseIdAsync(response))!.Value;
    }

    public async Task<int> CreateBoardAsync(int workspaceId, string name = "Playwright Board")
    {
        var response = await _request.PostAsync("boards", new() { DataObject = new { name, workspaceId } });
        await EnsureSuccessAsync(response);
        return (await ParseIdAsync(response))!.Value;
    }

    public async Task<int> CreateWorkItemAsync(
        int boardId,
        string name = "Playwright Task",
        string description = "Created by Playwright API test")
    {
        var response = await _request.PostAsync("work-items", new()
        {
            DataObject = new { name, description, boardId, priority = "Medium" }
        });
        await EnsureSuccessAsync(response);
        return (await ParseIdAsync(response))!.Value;
    }

    public async Task OpenWorkItemLockAsync(int workItemId)
    {
        var response = await _request.PostAsync($"work-item-locks/open/{workItemId}");
        await EnsureSuccessAsync(response);
    }

    public Task<IAPIResponse> GetWorkItemAsync(int workItemId) =>
        _request.GetAsync($"work-items/{workItemId}");

    public async Task<IAPIResponse> UpdateWorkItemAsync(
        int workItemId,
        int boardId,
        string name,
        string description)
    {
        await OpenWorkItemLockAsync(workItemId);
        return await _request.PutAsync($"work-items/{workItemId}", new()
        {
            DataObject = new { name, description, boardId, priority = "Medium" }
        });
    }

    public async Task<IAPIResponse> DeleteWorkItemAsync(int workItemId)
    {
        await OpenWorkItemLockAsync(workItemId);
        return await _request.DeleteAsync($"work-items/{workItemId}");
    }

    public async Task<int> CreateCommentAsync(int workItemId, string content)
    {
        await OpenWorkItemLockAsync(workItemId);

        var response = await _request.PostAsync("comments", new()
        {
            DataObject = new { workItemId, content }
        });
        await EnsureSuccessAsync(response);
        return (await ParseIdAsync(response))!.Value;
    }

    public Task<IAPIResponse> GetCommentsByWorkItemAsync(int workItemId) =>
        _request.GetAsync($"comments/work-item/{workItemId}");

    public Task<IAPIResponse> UpdateCommentAsync(int commentId, string content) =>
        _request.PutAsync($"comments/{commentId}", new() { DataObject = new { content } });

    public Task<IAPIResponse> DeleteCommentAsync(int commentId) =>
        _request.DeleteAsync($"comments/{commentId}");

    public Task<IAPIResponse> GetMyWorkspaceAsync() =>
        _request.GetAsync("workspace/my");

    public Task<IAPIResponse> UpdateWorkspaceAsync(int workspaceId, string name) =>
        _request.PutAsync($"workspace/{workspaceId}", new() { DataObject = new { name } });

    public Task<IAPIResponse> DeleteWorkspaceAsync(int workspaceId) =>
        _request.DeleteAsync($"workspace/{workspaceId}");

    public async Task<int> CreateSprintAsync(int boardId, string name = "Playwright Sprint")
    {
        var response = await _request.PostAsync("sprints", new()
        {
            DataObject = new
            {
                name,
                boardId,
                startDate = DateTime.UtcNow.Date,
                endDate = DateTime.UtcNow.Date.AddDays(7)
            }
        });
        await EnsureSuccessAsync(response);
        return (await ParseIdAsync(response))!.Value;
    }

    public Task<IAPIResponse> GetSprintAsync(int sprintId) =>
        _request.GetAsync($"sprints/{sprintId}");

    public Task<IAPIResponse> UpdateSprintAsync(int sprintId, string name) =>
        _request.PutAsync($"sprints/{sprintId}", new()
        {
            DataObject = new
            {
                name,
                startDate = DateTime.UtcNow.Date,
                endDate = DateTime.UtcNow.Date.AddDays(14)
            }
        });

    public Task<IAPIResponse> DeleteSprintAsync(int sprintId) =>
        _request.DeleteAsync($"sprints/{sprintId}");

    public async Task<TestEnvironment> CreateEnvironmentAsync(string workspaceName = "Playwright Workspace")
    {
        await AuthenticateAsync();
        var workspaceId = await CreateWorkspaceAsync(workspaceName);
        var boardId = await CreateBoardAsync(workspaceId);
        return new TestEnvironment
        {
            Email = Email,
            Password = Password,
            WorkspaceId = workspaceId,
            BoardId = boardId
        };
    }

    public async Task<int> CreateSubWorkItemAsync(
        int workItemId,
        string name = "Playwright Subtask",
        string description = "Created by Playwright API test")
    {
        var response = await _request.PostAsync("sub-work-items", new()
        {
            DataObject = new
            {
                name,
                description,
                workItemId,
                priority = "Medium"
            }
        });
        await EnsureSuccessAsync(response);
        return (await ParseIdAsync(response))!.Value;
    }

    //zatvara konekcije
    public async ValueTask DisposeAsync()
    {
        await _request.DisposeAsync();
    }

    private static async Task<IAPIRequestContext> NewRequestContextAsync(IPlaywright playwright, string? token = null)
    {
        var headers = new Dictionary<string, string>
        {
            { "Accept", "application/json" }
        };

        if (!string.IsNullOrEmpty(token))
        {
            headers["Authorization"] = $"Bearer {token}";
        }

        return await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = $"{TestSettings.ApiBaseUrl.TrimEnd('/')}/",
            ExtraHTTPHeaders = headers,
            IgnoreHTTPSErrors = true
        });
    }

    private static async Task EnsureSuccessAsync(IAPIResponse response)
    {
        if (!response.Ok)
        {
            var body = await response.TextAsync();
            throw new InvalidOperationException(
                $"API request failed: {response.Status} {response.StatusText}. Body: {body}");
        }
    }

    private static async Task<int?> ParseIdAsync(IAPIResponse response)
    {
        var body = await response.JsonAsync();
        return body is null ? null : body!.Value.GetProperty("id").GetInt32();
    }

    private static async Task<string> ExtractTokenAsync(IAPIResponse response)
    {
        var body = await response.JsonAsync()
            ?? throw new InvalidOperationException("Auth response was empty.");
        return body.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Auth token was not returned.");
    }
}

public sealed class TestEnvironment
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public int WorkspaceId { get; init; }
    public int BoardId { get; init; }
}
