using Microsoft.Playwright;

namespace PlaywrightTests.Helpers;

//koriste ga samo api testovi,  _playwright sluzi nam samo za http kod Apia 
[TestFixture]
public abstract class ApiTestBase
{
    private IPlaywright _playwright = null!;
    protected ApiClient Client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUpApi()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Client = await ApiClient.CreateAsync(_playwright);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownApi()
    {
        if (Client is not null)
            await Client.DisposeAsync();

        _playwright?.Dispose();
    }
}
