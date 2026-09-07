using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class AdminScanShould(BrowserFixture browser)
{
    private const string EventName = "FB Riders";
    private const string Buyer = "agent-checkout@redants.ch";

    [E2EFact]
    public async Task CheckATicketInAndOutFromTheTicketTable()
    {
        var page = await LoginAsync();
        await page.GotoAsync("/admin/ticketing?tab=tickets");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var eventSelect = page.Locator("#eventSelect");
        await Assertions.Expect(eventSelect).ToBeEnabledAsync(new() { Timeout = 30_000 });
        var eventValue = await eventSelect.Locator("option", new() { HasTextRegex = new Regex(EventName) }).First.GetAttributeAsync("value");
        Assert.False(string.IsNullOrEmpty(eventValue), $"No event option containing {EventName}");
        await eventSelect.SelectOptionAsync(eventValue!);

        await page.FillAsync("#ticketSearch", Buyer);
        await page.WaitForTimeoutAsync(1500);
        await browser.ShotAsync(page, "admin-scan-search");
        var row = page.Locator("tr", new() { HasTextRegex = new Regex(Buyer) }).First;
        await Assertions.Expect(row).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await browser.ShotAsync(page, "admin-scan-row");

        await ChangeRedemptionAsync(page, row, "1");
        await Assertions.Expect(row.Locator(".ta-badge-redeemed")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await browser.ShotAsync(page, "admin-scan-checked-in");

        await ChangeRedemptionAsync(page, row, "2");
        await Assertions.Expect(row.Locator(".ta-badge-blocked")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await browser.ShotAsync(page, "admin-scan-checked-out");
    }

    private static async Task ChangeRedemptionAsync(IPage page, ILocator row, string optionIndex)
    {
        await row.Locator("button.ta-inline[title='Eingelöst ändern']").ClickAsync();
        var editor = row.Locator("select.ta-inline-editor");
        await Assertions.Expect(editor).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await editor.SelectOptionAsync(optionIndex);
        var confirm = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Ändern$") });
        await Assertions.Expect(confirm).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await confirm.ClickAsync();
        await Assertions.Expect(confirm).ToBeHiddenAsync(new() { Timeout = 30_000 });
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
