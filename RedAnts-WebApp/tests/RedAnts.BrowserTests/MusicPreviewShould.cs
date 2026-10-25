using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class MusicPreviewShould(BrowserFixture browser)
{
    [E2EFact]
    public async Task PlayAPieceFromTheList()
    {
        var page = await OpenMusicAsync();
        var sound = page.WaitForResponseAsync(response => response.Url.Contains("/dj/sound/"), new() { Timeout = 20_000 });

        await page.Locator("button[data-preview]").First.ClickAsync();
        var response = await sound;

        await browser.ShotAsync(page, "music-preview-list");
        Assert.True(response.Status is 200 or 206, $"{response.Url} answered {response.Status}");
    }

    [E2EFact]
    public async Task PlayAPieceFromTheDialog()
    {
        var page = await OpenMusicAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Bearbeiten" }).First.ClickAsync();
        await page.Locator(".se-songrow-main").First.ClickAsync();
        var play = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Ganzes Stück") }).First;
        await Assertions.Expect(play).ToBeVisibleAsync(new() { Timeout = 20_000 });

        var sound = page.WaitForResponseAsync(response => response.Url.Contains("/dj/sound/"), new() { Timeout = 20_000 });
        await play.ClickAsync();
        var response = await sound;

        await browser.ShotAsync(page, "music-preview-dialog");
        Assert.True(response.Status is 200 or 206, $"{response.Url} answered {response.Status}");
    }

    private async Task<IPage> OpenMusicAsync()
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync("/umbraco/login");
        await page.Locator("input[name='username'], input[type='email'], #email-input").First.FillAsync(browser.AgentUserName);
        await page.Locator("input[type='password']").First.FillAsync(browser.AgentPassword);
        await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^(Anmelden|Login|Log in)$", RegexOptions.IgnoreCase) }).ClickAsync();
        await page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });

        await page.GotoAsync("/admin/show/musik");
        await Assertions.Expect(page.Locator("button[data-preview]").First).ToBeVisibleAsync(new() { Timeout = 30_000 });
        return page;
    }
}
