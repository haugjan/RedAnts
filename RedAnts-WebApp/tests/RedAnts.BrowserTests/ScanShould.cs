using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class ScanShould(BrowserFixture browser)
{
    [E2EFact]
    public async Task OpenTheEventListAfterHelperLogin()
    {
        var admin = await LoginAsync();
        await admin.GotoAsync("/admin/ticketing?tab=helper");
        await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var activeCode = admin.Locator("tr:has(input[type='checkbox']:checked) code.ta-uuid").First;
        await Assertions.Expect(activeCode).ToBeVisibleAsync(new() { Timeout = 30_000 });
        var code = (await activeCode.InnerTextAsync()).Trim();

        var page = await browser.NewPageAsync();
        await page.GotoAsync("/scan/login");
        await page.Locator("input[name='password']").FillAsync(code);
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Anmelden$") }).ClickAsync();
        await page.WaitForURLAsync(url => url.TrimEnd('/').EndsWith("/scan"), new() { Timeout = 30_000 });
        await Assertions.Expect(page.Locator(".scan-app__header")).ToContainTextAsync("Tickets scannen", new() { Timeout = 30_000 });
        await Assertions.Expect(page.Locator(".scan-app")).Not.ToContainTextAsync("Nur angemeldete Helfer");
        await browser.ShotAsync(page, "scan-events");
    }

    [E2EFact]
    public async Task WarmTheScannerPage()
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync("/warmup");
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("/scan -> 200", new() { Timeout = 60_000 });
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("scanner data -> ok");
    }

    private async Task<IPage> LoginAsync()
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync("/umbraco/login");
        await page.Locator("input[name='username'], input[type='email'], #email-input").First.FillAsync(browser.AgentUserName);
        await page.Locator("input[type='password']").First.FillAsync(browser.AgentPassword);
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^(Anmelden|Login|Log in)$", RegexOptions.IgnoreCase) }).ClickAsync();
        await page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
        await Assertions.Expect(page.Locator("umb-backoffice, umb-app")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        return page;
    }
}
