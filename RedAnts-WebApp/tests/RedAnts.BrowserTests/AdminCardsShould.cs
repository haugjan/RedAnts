using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class AdminCardsShould(BrowserFixture browser)
{
    [E2ETheory]
    [InlineData("seasoncards", "Saisonkarten")]
    [InlineData("membercards", "Mitglieder")]
    [InlineData("flextickets", "Flextickets")]
    [InlineData("helper", "Helfer")]
    public async Task RenderTheTab(string tab, string heading)
    {
        var page = await LoginAsync();
        await page.GotoAsync($"/admin/ticketing?tab={tab}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync(heading, new() { Timeout = 30_000 });
        await Assertions.Expect(page.Locator(".ta-modal-error, .blazor-error-boundary")).ToHaveCountAsync(0);
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
