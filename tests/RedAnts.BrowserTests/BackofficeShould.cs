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

        await page.Locator("input[name='username'], input[type='email'], #email-input").First.FillAsync(browser.AgentUserName);
        await page.Locator("input[type='password']").First.FillAsync(browser.AgentPassword);
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^(Anmelden|Login|Log in)$", RegexOptions.IgnoreCase) }).ClickAsync();
        try
        {
            await page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
            await Assertions.Expect(page.Locator("umb-backoffice, umb-app")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        }
        catch
        {
            await browser.ShotAsync(page, "backoffice-login-failed");
            throw;
        }
        return page;
    }
}
