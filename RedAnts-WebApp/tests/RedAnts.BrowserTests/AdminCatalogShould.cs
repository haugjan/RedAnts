using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class AdminCatalogShould(BrowserFixture browser)
{
    [E2EFact]
    public async Task ListTheEventsOfTheSeason()
    {
        var page = await LoginAsync();
        await page.GotoAsync("/admin/ticketing?tab=events");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("FB Riders", new() { Timeout = 30_000 });
        await browser.ShotAsync(page, "admin-events");
        Assert.Equal(0, await page.Locator("button.ta-inline[title='Bezeichnung ändern']").CountAsync());
    }

    [E2EFact]
    public async Task ListTheSeasonsWithTheirQuotas()
    {
        var page = await LoginAsync();
        await page.GotoAsync("/admin/ticketing?tab=seasons");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("Saison 2026/27", new() { Timeout = 30_000 });
        await browser.ShotAsync(page, "admin-seasons");
        Assert.Equal(0, await page.Locator("button.ta-inline[title='Saisonbezeichnung ändern']").CountAsync());
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
