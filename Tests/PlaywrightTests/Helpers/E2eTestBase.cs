using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using PlaywrightTests.Configuration;

namespace PlaywrightTests.Helpers;

public abstract class E2eTestBase : PageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize
            {
                Width = 1280,
                Height = 720
            }
        };

        if (TestSettings.RecordVideo)
        {
            options.RecordVideoSize = new RecordVideoSize
            {
                Width = 1280,
                Height = 720
            };
            options.RecordVideoDir = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "Videos");
        }

        return options;
    }

    protected Task<ApiClient> CreateApiClientAsync() =>
        ApiClient.CreateAsync(Playwright);
}
