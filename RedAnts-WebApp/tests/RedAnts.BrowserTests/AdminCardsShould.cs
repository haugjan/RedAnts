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

    private async Task<IPage> OpenTabAsync(string tab)
    {
        var page = await LoginAsync();
        await page.GotoAsync($"/admin/ticketing?tab={tab}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator(".ta-toolbar-actions")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        return page;
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
