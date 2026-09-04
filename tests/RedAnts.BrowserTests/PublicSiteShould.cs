using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class PublicSiteShould(BrowserFixture browser)
{
    [E2EFact]
    public async Task AnswerHealth()
    {
        var page = await browser.NewPageAsync();
        var response = await page.GotoAsync("/health");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);
    }

    [E2EFact]
    public async Task ShowTheTicketingHome()
    {
        var page = await browser.NewPageAsync();
        var response = await page.GotoAsync("/ticketing/");
        await browser.ShotAsync(page, "public-ticketing-home");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("Red Ants");
    }

    [E2EFact]
    public async Task ShowTheSeasonList()
    {
        var page = await browser.NewPageAsync();
        var response = await page.GotoAsync("/seasons/");
        await browser.ShotAsync(page, "public-seasons");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);
    }
}
