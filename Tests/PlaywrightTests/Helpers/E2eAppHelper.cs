using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightTests.Helpers;

//helper UI akcija za e2e 
public static class E2eAppHelper
{
    public static Task LoginAsync(IPage page, TestEnvironment env) =>
        AuthHelper.LoginAsync(page, env.Email, env.Password);

    public static Task LoginAsync(IPage page, string email, string password) =>
        AuthHelper.LoginAsync(page, email, password);

    public static async Task EnsureBacklogTabAsync(IPage page)
    {
        await page
            .Locator(".toolbar-item")
            .Filter(new() { HasText = "Backlog" })
            .ClickAsync();
    }

    public static ILocator TaskRow(IPage page, string taskName) =>
        page.Locator(".task-item").Filter(new() { HasText = taskName });

    public static ILocator TopmostModal(IPage page, string titleFragment) =>
        page
            .Locator(".modal-container")
            .Filter(new() { Has = page.Locator(".modal-header-title", new() { HasText = titleFragment }) })
            .Last;

    public static async Task OpenTaskModalAsync(IPage page, string taskName)
    {
        await EnsureBacklogTabAsync(page);
        await TaskRow(page, taskName).Locator(".task-title").ClickAsync();
        await Expect(TopmostModal(page, "Edit Task").Locator(".modal-header-title"))
            .ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    public static async Task CreateTaskFromUiAsync(
        IPage page,
        string title,
        string description)
    {
        await page
            .Locator(".users-container-add-button")
            .Filter(new() { Has = page.Locator("svg-icon.users-container-add-button-icon") })
            .ClickAsync();

        var modal = TopmostModal(page, "Add Task");
        await modal.GetByPlaceholder("Enter task title...").FillAsync(title);
        await modal.GetByPlaceholder("Enter description...").FillAsync(description);
        await modal.Locator(".modal-footer .action-button").ClickAsync();

        await Expect(PageGetTaskTitle(page, title)).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    public static async Task UpdateOpenTaskAsync(IPage page, string newTitle, string newDescription)
    {
        var modal = TopmostModal(page, "Edit Task");
        await modal.Locator(".title-input").FillAsync(newTitle);
        await modal.Locator(".description-textarea").FillAsync(newDescription);
        await modal.GetByText("Save changes", new() { Exact = true }).ClickAsync();

        await Expect(modal).ToHaveCountAsync(0, new() { Timeout = 15000 });
    }

    public static async Task DeleteTaskAsync(IPage page, ApiClient client, int workItemId, string taskName)
    {
        await client.OpenWorkItemLockAsync(workItemId);

        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

        await TaskRow(page, taskName).Locator(".task-menu").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Expect(TaskRow(page, taskName)).ToHaveCountAsync(0, new() { Timeout = 15000 });
    }

    public static async Task AddCommentInOpenTaskAsync(IPage page, string commentText)
    {
        var modal = TopmostModal(page, "Edit Task");
        await modal.GetByPlaceholder("Write a comment...").FillAsync(commentText);
        await modal.Locator(".task-comments__send").ClickAsync();

        await Expect(modal.Locator(".task-comment__content", new() { HasText = commentText }))
            .ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    public static async Task EditCommentInOpenTaskAsync(
        IPage page,
        string originalText,
        string updatedText)
    {
        var modal = TopmostModal(page, "Edit Task");
        await Expect(modal.Locator(".task-comments__loading")).ToHaveCountAsync(0, new() { Timeout = 15000 });

        var commentBlock = modal.Locator(".task-comment").Filter(new() { HasText = originalText });
        await Expect(commentBlock.Locator(".task-comment__content")).ToBeVisibleAsync(new() { Timeout = 15000 });

        var editButton = commentBlock.GetByText("Edit", new() { Exact = true });
        await Expect(editButton).ToBeEnabledAsync(new() { Timeout = 15000 });
        await editButton.ClickAsync();

        var editInput = modal.Locator(".task-comment__edit-input");
        await Expect(editInput).ToBeVisibleAsync(new() { Timeout = 15000 });
        await editInput.FillAsync(updatedText);
        await modal.Locator(".task-comment__btn--primary", new() { HasText = "Save" }).ClickAsync();

        await Expect(modal.Locator(".task-comment__content", new() { HasText = updatedText }))
            .ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    public static async Task DeleteCommentInOpenTaskAsync(IPage page, string commentText)
    {
        var modal = TopmostModal(page, "Edit Task");
        var comment = modal.Locator(".task-comment").Filter(new() { HasText = commentText });
        await comment.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Expect(modal.Locator(".task-comment__content", new() { HasText = commentText }))
            .ToHaveCountAsync(0, new() { Timeout = 15000 });
    }

    public static async Task CreateSubtaskInOpenTaskAsync(
        IPage page,
        string title,
        string description)
    {
        var parentModal = TopmostModal(page, "Edit Task");
        await parentModal.GetByRole(AriaRole.Button, new() { Name = "Create Subtask" }).ClickAsync();

        var subtaskModal = TopmostModal(page, "Add Subtask");
        await Expect(subtaskModal).ToBeVisibleAsync();
        await subtaskModal.GetByPlaceholder("Enter subtask title...").FillAsync(title);
        await subtaskModal.GetByPlaceholder("Enter description...").FillAsync(description);
        await subtaskModal.GetByText("Create subtask", new() { Exact = true }).ClickAsync();

        await Expect(parentModal.Locator(".task-subtasks__name", new() { HasText = title }))
            .ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    public static async Task UpdateSubtaskInOpenTaskAsync(
        IPage page,
        string currentTitle,
        string updatedTitle)
    {
        var parentModal = TopmostModal(page, "Edit Task");
        await parentModal
            .Locator(".task-subtasks__row")
            .Filter(new() { HasText = currentTitle })
            .ClickAsync();

        var subtaskModal = TopmostModal(page, "Edit Subtask");
        await Expect(subtaskModal).ToBeVisibleAsync();
        await subtaskModal.GetByPlaceholder("Enter subtask title...").FillAsync(updatedTitle);
        await subtaskModal.GetByText("Save changes", new() { Exact = true }).ClickAsync();

        await Expect(parentModal.Locator(".task-subtasks__name", new() { HasText = updatedTitle }))
            .ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    public static async Task FillLabeledInputAsync(IPage page, string label, string value)
    {
        var input = page.GetByLabel(label);
        await input.ClickAsync();
        await input.FillAsync(value);
        await input.BlurAsync();
    }

    private static ILocator PageGetTaskTitle(IPage page, string title) =>
        page.Locator(".task-title", new() { HasText = title });
}
