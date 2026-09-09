using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class AdminCardsShould(BrowserFixture browser)
{
    private static readonly Regex BundleTicketCount = new(@"(\d+)\s*Stk\.", RegexOptions.CultureInvariant);

    [E2ETheory]
    [InlineData("seasoncards", "Saisonkarten")]
    [InlineData("membercards", "Mitglieder")]
    [InlineData("flextickets", "Flextickets")]
    [InlineData("helper", "Helfer")]
    public async Task RenderTheTab(string tab, string heading)
    {
        var page = await OpenTabAsync(tab);
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync(heading, new() { Timeout = 30_000 });
        await Assertions.Expect(page.Locator(".ta-modal-error, .blazor-error-boundary")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("body")).Not.ToContainTextAsync(".Handler ");
        await browser.ShotAsync(page, $"admin-{tab}");
    }

    [E2ETheory]
    [InlineData("seasoncards", "Saisonkarte erstellen", "Saisonkarte erstellen", "create")]
    [InlineData("seasoncards", "Import CSV", "Saisonkarten importieren", "import")]
    [InlineData("seasoncards", "Export CSV", "Saisonkarten exportieren", "export")]
    [InlineData("seasoncards", "Karten versenden", "Karten versenden", "bundlemail")]
    [InlineData("membercards", "Mitgliederkarte erstellen", "Mitgliederkarte erstellen", "create")]
    [InlineData("membercards", "Import CSV", "Mitglieder importieren", "import")]
    [InlineData("membercards", "Export CSV", "Mitglieder exportieren (CSV)", "export")]
    [InlineData("membercards", "Karten versenden", "Karten versenden", "bundlemail")]
    public async Task OpenAToolbarDialog(string tab, string button, string title, string shot)
    {
        var page = await OpenTabAsync(tab);
        var trigger = page.Locator(".ta-toolbar-actions button", new() { HasTextString = button }).First;
        await OpenDialogAsync(page, trigger, title);
        await browser.ShotAsync(page, $"admin-{tab}-{shot}");
        await CloseWithEscapeAsync(page);
    }

    [E2ETheory]
    [InlineData("seasoncards", "Bearbeiten", "Saisonkarte bearbeiten", "edit")]
    [InlineData("seasoncards", "E-Mail senden", "Saisonkarte senden", "mail")]
    [InlineData("seasoncards", "Als PDF drucken", "Saisonkarten als PDF drucken", "print")]
    [InlineData("seasoncards", "Löschen", "Saisonkarte löschen", "delete")]
    [InlineData("membercards", "Bearbeiten", "Mitglied bearbeiten", "edit")]
    [InlineData("membercards", "E-Mail senden", "Mitgliederkarte senden", "mail")]
    [InlineData("membercards", "Als PDF drucken", "Mitgliederkarten als PDF drucken", "print")]
    [InlineData("membercards", "Löschen", "Mitgliederkarte löschen", "delete")]
    public async Task OpenARowDialog(string tab, string buttonTitle, string title, string shot)
    {
        var page = await OpenTabAsync(tab);
        var trigger = page.Locator($".ta-table tbody button[title='{buttonTitle}']:not([disabled])").First;
        await Assertions.Expect(trigger).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await OpenDialogAsync(page, trigger, title);
        await browser.ShotAsync(page, $"admin-{tab}-{shot}");
        await CloseWithEscapeAsync(page);
    }

    [E2ETheory]
    [InlineData("tickets", "Ticket erstellen", "Ticket erstellen")]
    [InlineData("tickets", "Bundle erstellen", "Ticket-Bundle erstellen")]
    [InlineData("tickets", "Import CSV", "Spieltickets importieren")]
    [InlineData("tickets", "Export CSV", "Spieltickets exportieren")]
    [InlineData("tickets", "Tickets versenden", "Spieltickets versenden")]
    [InlineData("flextickets", "Einzelticket erstellen", "Flexticket erstellen")]
    [InlineData("flextickets", "Bundle erstellen", "Flexticket-Bundle erstellen")]
    [InlineData("flextickets", "Import CSV", "Flextickets importieren")]
    [InlineData("flextickets", "Export CSV", "Bundles exportieren")]
    [InlineData("flextickets", "Tickets versenden", "Flextickets versenden")]
    [InlineData("flextickets", "Bundle bearbeiten", "Bundle bearbeiten")]
    [InlineData("flextickets", "PDF drucken", "Flextickets als PDF drucken")]
    public async Task OpenTheToolbarDialogAndCancelWithEscape(string tab, string button, string heading)
    {
        var page = await OpenTabAsync(tab);
        await page.Locator(".ta-toolbar-actions").GetByRole(AriaRole.Button, new() { Name = button, Exact = true }).ClickAsync();
        await ExpectDialogAsync(page, heading);
        await browser.ShotAsync(page, $"dialog-{tab}-{Slug(button)}");
        await CancelWithEscapeAsync(page);
    }

    [E2ETheory]
    [InlineData("tickets", "E-Mail senden", "Spielticket senden")]
    [InlineData("tickets", "Bearbeiten", "Ticket bearbeiten")]
    [InlineData("tickets", "Als PDF drucken", "Spieltickets als PDF drucken")]
    [InlineData("tickets", "Löschen", "Ticket löschen")]
    [InlineData("flextickets", "E-Mail senden", "Flexticket senden")]
    [InlineData("flextickets", "Personendaten bearbeiten", "Flexticket bearbeiten")]
    [InlineData("flextickets", "Als PDF drucken", "Flextickets als PDF drucken")]
    [InlineData("flextickets", "Löschen", "Flexticket löschen")]
    [InlineData("flextickets", "Einlösung ändern", "Einlösung ändern")]
    public async Task OpenTheRowDialogAndCancelWithEscape(string tab, string title, string heading)
    {
        var page = await OpenTabAsync(tab);
        await page.Locator($"tbody [title='{title}']").First.ClickAsync();
        await ExpectDialogAsync(page, heading);
        await browser.ShotAsync(page, $"dialog-{tab}-row-{Slug(title)}");
        await CancelWithEscapeAsync(page);
    }

    private static string Slug(string text) => Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

    private static async Task ExpectDialogAsync(IPage page, string heading)
    {
        await Assertions.Expect(page.Locator(".ta-overlay .ta-modal-title")).ToHaveTextAsync(heading, new() { Timeout = 60_000 });
        await Assertions.Expect(page.Locator(".blazor-error-boundary")).ToHaveCountAsync(0);
    }

    private static async Task CancelWithEscapeAsync(IPage page)
    {
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".ta-overlay")).ToHaveCountAsync(0, new() { Timeout = 30_000 });
    }

    private async Task<IPage> OpenTabAsync(string tab)
    {
        var page = await LoginAsync();
        await page.GotoAsync($"/admin/ticketing?tab={tab}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator(".ta-toolbar-actions")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        if (tab is "tickets" or "flextickets") await SelectSmallestBundleAsync(page);
        return page;
    }

    private static async Task SelectSmallestBundleAsync(IPage page)
    {
        var select = page.Locator("#bundleSelect");
        if (await select.CountAsync() == 0) return;

        var smallestValue = "";
        var smallestCount = int.MaxValue;
        foreach (var option in await select.Locator("option").AllAsync())
        {
            var value = await option.GetAttributeAsync("value") ?? "";
            var match = BundleTicketCount.Match(await option.InnerTextAsync());
            if (value == "0" || !match.Success) continue;
            var count = int.Parse(match.Groups[1].Value);
            if (count > 0 && count < smallestCount) { smallestCount = count; smallestValue = value; }
        }
        if (smallestValue.Length == 0) return;

        await select.SelectOptionAsync(new SelectOptionValue { Value = smallestValue });
        await Assertions.Expect(page.Locator("tbody tr")).ToHaveCountAsync(smallestCount, new() { Timeout = 60_000 });
    }

    private static async Task OpenDialogAsync(IPage page, ILocator trigger, string title)
    {
        var heading = page.Locator(".ta-overlay .ta-modal-title").First;
        for (var attempt = 1; attempt <= 6; attempt++)
        {
            await trigger.ClickAsync();
            try
            {
                await heading.WaitForAsync(new() { Timeout = 5_000 });
                break;
            }
            catch (PlaywrightException) when (attempt < 6)
            {
            }
        }
        await Assertions.Expect(heading).ToHaveTextAsync(title, new() { Timeout = 15_000 });
    }

    private static async Task CloseWithEscapeAsync(IPage page)
    {
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".ta-overlay")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
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
