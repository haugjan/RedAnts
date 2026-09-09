using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class ShowShould(BrowserFixture browser)
{
    [E2EFact]
    public async Task RenderTheBoard()
    {
        var page = await browser.NewPageAsync();
        var response = await page.GotoAsync("/show");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await browser.ShotAsync(page, "show-board");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);
        await Assertions.Expect(page.Locator("body")).Not.ToContainTextAsync("An unhandled error has occurred", new() { Timeout = 30_000 });
    }

    [E2EFact]
    public async Task GuardTheStateApiWithTheShowKey()
    {
        var page = await browser.NewPageAsync();
        var response = await page.GotoAsync("/api/show/state");

        Assert.NotNull(response);
        Assert.Equal(401, response!.Status);
    }

    [E2EFact]
    public async Task RenderTheAdminPage()
    {
        var page = await OpenAdminAsync();
        await browser.ShotAsync(page, "show-admin");

        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Soundboard-Konfiguration");
        await Assertions.Expect(page.Locator(".se-canvas")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".blazor-error-boundary")).ToHaveCountAsync(0);
    }

    [E2EFact]
    public async Task OpenAndCancelTheSpotifySettingsDialog()
    {
        var page = await OpenAdminAsync();
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Spotify") }).First.ClickAsync();

        await Assertions.Expect(page.Locator(".se-spotifycard")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.Locator(".se-spotifyh")).ToContainTextAsync("Spotify-Zugang");
        await browser.ShotAsync(page, "show-admin-spotify-settings");

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".se-spotifycard")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    [E2EFact]
    public async Task OpenAndCloseTheProfileBundlePanel()
    {
        var page = await OpenAdminAsync();
        var toggle = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Import") });
        await toggle.ClickAsync();

        await Assertions.Expect(page.Locator(".se-io")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.Locator(".se-io")).ToContainTextAsync("Bundle exportieren");
        await browser.ShotAsync(page, "show-admin-bundle-panel");

        await toggle.ClickAsync();
        await Assertions.Expect(page.Locator(".se-io")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    [E2EFact]
    public async Task OpenAndCancelTheNewTileDialog()
    {
        var page = await OpenAdminAsync();
        var cell = await FreeCellAsync(page);
        if (cell is null)
        {
            await page.Locator(".se-ct.folder .se-ct-open").First.ClickAsync();
            await Assertions.Expect(page.Locator(".se-reserved").First).ToContainTextAsync("Zurück", new() { Timeout = 15_000 });
            cell = await FreeCellAsync(page);
        }

        Assert.NotNull(cell);
        await page.Locator(".se-canvas").ClickAsync(new() { Position = new Position { X = cell![0], Y = cell[1] } });

        await Assertions.Expect(page.Locator(".se-choosecard")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.Locator(".se-choosecard h3")).ToContainTextAsync("Neue Kachel");
        await browser.ShotAsync(page, "show-admin-new-tile");

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".se-choosecard")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    [E2EFact]
    public async Task OpenAndCancelTheTileEditorDialog()
    {
        var page = await OpenAdminAsync();
        var tile = page.Locator(".se-ct:not(.folder)").First;
        var label = (await tile.Locator(".se-ct-lbl").InnerTextAsync()).Trim();
        await tile.ClickAsync();

        await Assertions.Expect(page.Locator(".se-editcard")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(page.Locator(".se-pv-label")).ToContainTextAsync(label);
        await browser.ShotAsync(page, "show-admin-tile-editor");

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".se-editcard")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    [E2EFact]
    public async Task AskBeforeDiscardingATileWithoutASong()
    {
        var page = await OpenAdminAsync();
        await page.Locator(".se-add-tiles").GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Song") }).ClickAsync();
        await Assertions.Expect(page.Locator(".se-editcard")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".se-choosecard h3")).ToContainTextAsync("Kein Song gewählt", new() { Timeout = 15_000 });
        await browser.ShotAsync(page, "show-admin-discard-tile");

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".se-choosecard")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(page.Locator(".se-editcard")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Abbrechen" }).ClickAsync();
        await Assertions.Expect(page.Locator(".se-editcard")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private static async Task<float[]?> FreeCellAsync(IPage page) =>
        await page.EvaluateAsync<float[]?>(@"() => {
            const canvas = document.querySelector('.se-canvas');
            if (!canvas) return null;
            const box = canvas.getBoundingClientRect();
            const pad = 6, gap = 6, cols = 5, rows = 3;
            const cw = (box.width - pad * 2 - gap * (cols - 1)) / cols;
            const ch = (box.height - pad * 2 - gap * (rows - 1)) / rows;
            const taken = new Set();
            canvas.querySelectorAll('.se-ct').forEach(t =>
                taken.add(t.getAttribute('data-x') + ',' + t.getAttribute('data-y')));
            canvas.querySelectorAll('.se-reserved').forEach(t => {
                const col = /grid-column:\s*(\d+)/.exec(t.getAttribute('style') || '');
                const row = /grid-row:\s*(\d+)/.exec(t.getAttribute('style') || '');
                if (col && row) taken.add((+col[1] - 1) + ',' + (+row[1] - 1));
            });
            for (let y = 0; y < rows; y++)
                for (let x = 0; x < cols; x++)
                    if (!taken.has(x + ',' + y))
                        return [pad + x * (cw + gap) + cw / 2, pad + y * (ch + gap) + ch / 2];
            return null;
        }");

    private async Task<IPage> OpenAdminAsync()
    {
        var page = await LoginAsync();
        await page.GotoAsync("/admin/show");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator(".se-canvas")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        return page;
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
