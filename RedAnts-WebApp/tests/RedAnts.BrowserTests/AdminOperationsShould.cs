using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class AdminOperationsShould(BrowserFixture browser)
{
    [E2ETheory]
    [InlineData("tickets", "Spieltickets")]
    [InlineData("freeentries", "Freier Einlass")]
    [InlineData("addons", "Zusatzoptionen")]
    [InlineData("stats", "Statistik")]
    [InlineData("newsletter", "Newsletter")]
    [InlineData("outbox", "E-Mails")]
    public async Task RenderTheOperationsTab(string tab, string title)
    {
        var page = await LoginAsync();
        await page.GotoAsync($"/admin/ticketing?tab={tab}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator("h1.ta-page-title")).ToContainTextAsync(title, new() { Timeout = 30_000 });
        await Assertions.Expect(page.Locator("section.ta-page")).Not.ToContainTextAsync("Wird geladen", new() { Timeout = 30_000 });
        Assert.Equal(0, await page.Locator("#blazor-error-ui:visible").CountAsync());
        await browser.ShotAsync(page, $"admin-{tab}");
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
