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
}
