using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class BackofficeShould(BrowserFixture browser)
{
    [E2EFact]
    public async Task LetTheAgentUserLogInWithThePasswordForm()
    {
        var page = await LoginAsync();
        await browser.ShotAsync(page, "backoffice-after-login");

        await Assertions.Expect(page).ToHaveURLAsync(new Regex("/umbraco(/|$|#)"));
        await Assertions.Expect(page.Locator("umb-backoffice, umb-app")).ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    [E2EFact]
    public async Task OpenTheTicketingAdmin()
    {
        var page = await LoginAsync();
        var response = await page.GotoAsync("/admin/ticketing");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await browser.ShotAsync(page, "admin-ticketing");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("Anlässe", new() { Timeout = 30_000 });
    }

    private async Task<IPage> LoginAsync()
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync("/umbraco/login");
        await browser.ShotAsync(page, "backoffice-login");

        await page.GetByLabel(new Regex("E-Mail|Email|Benutzername|Username", RegexOptions.IgnoreCase)).FillAsync(browser.AgentUserName);
        await page.GetByLabel(new Regex("Passwort|Password", RegexOptions.IgnoreCase)).FillAsync(browser.AgentPassword);
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Anmelden|Login|Log in", RegexOptions.IgnoreCase) }).ClickAsync();
        await page.WaitForURLAsync(new Regex("/umbraco(/|$|#)"), new() { Timeout = 30_000 });
        return page;
    }
}
