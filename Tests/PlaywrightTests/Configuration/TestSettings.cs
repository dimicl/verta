using Microsoft.Extensions.Configuration;

namespace PlaywrightTests.Configuration;

public static class TestSettings
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables()
        .Build();

    public static string ApiBaseUrl =>
        Configuration["ApiBaseUrl"] ?? "http://localhost:8080/api";

    public static string FrontendBaseUrl =>
        Configuration["FrontendBaseUrl"] ?? "http://localhost:4210";

    public static string TestEmail =>
        Configuration["TestEmail"] ?? "test@verta.com";

    public static string TestPassword =>
        Configuration["TestPassword"] ?? "Test123!";

    public static bool RecordVideo =>
        bool.TryParse(Configuration["RecordVideo"], out var record) && record;
}
