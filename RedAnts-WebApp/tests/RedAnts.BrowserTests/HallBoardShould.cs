using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class HallBoardShould(BrowserFixture browser)
{
    [E2EFact]
    public async Task RenderTheBoardOfAHall()
    {
        var page = await browser.NewPageAsync();
        var response = await page.GotoAsync("/bord");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await browser.ShotAsync(page, "hall-board");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);
        await Assertions.Expect(page.Locator("body")).Not.ToContainTextAsync("An unhandled error has occurred", new() { Timeout = 30_000 });
    }

    [E2EFact]
    public async Task OpenTheGameMask()
    {
        var page = await browser.NewPageAsync();
        var response = await page.GotoAsync("/bord?spiel=1");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await browser.ShotAsync(page, "hall-board-game");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);
        await Assertions.Expect(page.Locator("body")).Not.ToContainTextAsync("An unhandled error has occurred", new() { Timeout = 30_000 });
    }
}
