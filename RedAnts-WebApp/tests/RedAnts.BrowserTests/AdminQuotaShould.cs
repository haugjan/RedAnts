using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class AdminQuotaShould(BrowserFixture browser)
{
    private const string EventName = "FB Riders";

    [E2EFact]
    public async Task RejectASalesQuotaAboveTheAdmissionQuota()
    {
        var page = await LoginAsync();
        await page.GotoAsync("/admin/ticketing?tab=events");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var row = page.Locator("tr", new() { HasTextRegex = new Regex(EventName) }).First;
        await Assertions.Expect(row).ToBeVisibleAsync(new() { Timeout = 30_000 });

        var before = (await row.Locator("button.ta-inline[title='Verkaufskontingent ändern']").InnerTextAsync()).Trim();
        await row.Locator("button.ta-inline[title='Verkaufskontingent ändern']").ClickAsync();
        var editor = row.Locator("input.ta-inline-num");
        await Assertions.Expect(editor).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await editor.FillAsync("99999");
        await editor.PressAsync("Enter");

        var confirm = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Ändern$") });
        await Assertions.Expect(confirm).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await confirm.ClickAsync();
        await browser.ShotAsync(page, "admin-quota-rejected");

        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("Einlasskontingent nicht übersteigen", new() { Timeout = 15_000 });
        await page.Keyboard.PressAsync("Escape");
        await page.ReloadAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var after = (await page.Locator("tr", new() { HasTextRegex = new Regex(EventName) }).First
            .Locator("button.ta-inline[title='Verkaufskontingent ändern']").InnerTextAsync()).Trim();
        Assert.Equal(before, after);
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
