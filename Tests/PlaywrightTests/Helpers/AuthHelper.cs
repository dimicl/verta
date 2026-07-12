using Microsoft.Playwright;
using PlaywrightTests.Configuration;

namespace PlaywrightTests.Helpers;

public static class AuthHelper
{
    public static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{TestSettings.FrontendBaseUrl}/login");
        await page.GetByLabel("Email").FillAsync(email);
        await page.GetByLabel("Password").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await page.WaitForURLAsync(url => !url.Contains("/login"), new() { Timeout = 15000 });
    }
}
