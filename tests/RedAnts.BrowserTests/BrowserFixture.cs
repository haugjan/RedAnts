using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

public sealed class BrowserFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public string BaseUrl { get; } = (Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5606").TrimEnd('/');

    public string ScreenshotDirectory { get; } =
        Environment.GetEnvironmentVariable("E2E_SCREENSHOT_DIR")
        ?? Path.Combine(AppContext.BaseDirectory, "screenshots");

    public IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .AddUserSecrets<BrowserFixture>(optional: true)
        .AddEnvironmentVariables()
        .Build();

    public string AgentUserName => Configuration["Agent:BackofficeUser"] ?? "agent@redants.ch";

    public string AgentPassword => Configuration["Agent:BackofficePassword"]
        ?? throw new InvalidOperationException("Agent:BackofficePassword is not configured (user secrets or environment).");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(ScreenshotDirectory);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task<IPage> NewPageAsync()
    {
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            Locale = "de-CH"
        });
        var page = await context.NewPageAsync();
        var eventLog = Path.Combine(ScreenshotDirectory, "browser-events.log");
        page.Console += (_, message) =>
        {
            if (message.Type is "error" or "warning")
                File.AppendAllText(eventLog, $"{DateTime.Now:HH:mm:ss} console.{message.Type} {page.Url} {message.Text}{Environment.NewLine}");
        };
        page.PageError += (_, error) =>
            File.AppendAllText(eventLog, $"{DateTime.Now:HH:mm:ss} pageerror {page.Url} {error}{Environment.NewLine}");
        page.RequestFailed += (_, request) =>
            File.AppendAllText(eventLog, $"{DateTime.Now:HH:mm:ss} requestfailed {request.Method} {request.Url} {request.Failure}{Environment.NewLine}");
        page.Response += (_, response) =>
        {
            if (response.Status >= 400)
                File.AppendAllText(eventLog, $"{DateTime.Now:HH:mm:ss} response {response.Status} {response.Request.Method} {response.Url}{Environment.NewLine}");
        };
        return page;
    }

    public async Task ShotAsync(IPage page, string name)
    {
        var path = Path.Combine(ScreenshotDirectory, $"{name}.png");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}

[CollectionDefinition(Name)]
public sealed class BrowserCollection : ICollectionFixture<BrowserFixture>
{
    public const string Name = "Browser";
}
