using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class AdminOrdersShould(BrowserFixture browser)
{
    [E2EFact]
    public async Task ListTheOrdersOfTheSeason()
    {
        var page = await LoginAsync();
        await page.GotoAsync("/admin/ticketing?tab=orders");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("AGT-", new() { Timeout = 30_000 });
        await browser.ShotAsync(page, "admin-orders");
    }

    [E2EFact]
    public async Task OpenTheRefundDialogOfAPaidOrder()
    {
        var page = await LoginAsync();
        await page.GotoAsync("/admin/ticketing?tab=orders");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator("table")).ToBeVisibleAsync(new() { Timeout = 30_000 });

        var refund = page.GetByRole(AriaRole.Button, new() { Name = "Rückerstatten" }).First;
        await Assertions.Expect(refund).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await refund.ClickAsync();

        await Assertions.Expect(page.Locator(".ta-modal-title")).ToContainTextAsync("Rückerstattung", new() { Timeout = 30_000 });
        await browser.ShotAsync(page, "admin-orders-refund");
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".ta-modal-title")).ToHaveCountAsync(0, new() { Timeout = 30_000 });
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
