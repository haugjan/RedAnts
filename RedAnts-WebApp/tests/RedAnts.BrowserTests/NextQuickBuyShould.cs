using System.Globalization;
using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class NextQuickBuyShould(BrowserFixture browser)
{
    [E2EFact]
    public async Task AddUpTheChosenTicketsPerCategory()
    {
        var page = await OpenAsync();
        var rows = page.Locator("[data-nq-row]");
        await Assertions.Expect(rows.First).ToBeVisibleAsync(new() { Timeout = 30_000 });

        var first = rows.First;
        var second = rows.Nth(1);
        var plus = second.Locator("[data-nq-step='1']");
        await plus.ClickAsync();
        await plus.ClickAsync();

        var expected = await PriceOfAsync(first) * await QuantityOfAsync(first) + await PriceOfAsync(second) * 2;

        await Assertions.Expect(page.Locator("#nqBuyLabel"))
            .ToContainTextAsync(expected.ToString("0.00", CultureInfo.InvariantCulture));
        await Assertions.Expect(second).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("nq-cat--active"));
        await browser.ShotAsync(page, "next-quickbuy");
    }

    [E2EFact]
    public async Task StopCountingDownAtZero()
    {
        var page = await OpenAsync();
        var row = page.Locator("[data-nq-row]").First;
        await Assertions.Expect(row).ToBeVisibleAsync(new() { Timeout = 30_000 });

        var minus = row.Locator("[data-nq-step='-1']");
        for (var click = await QuantityOfAsync(row); click > 0; click--)
            await minus.ClickAsync();

        await Assertions.Expect(minus).ToBeDisabledAsync();
        await Assertions.Expect(page.Locator("#nqBuy")).ToBeDisabledAsync();
        await Assertions.Expect(page.Locator("#nqBuyLabel")).ToContainTextAsync("Anzahl wählen");
    }

    [E2EFact]
    public async Task KeepTheBuyButtonOnScreenOnASmallPhone()
    {
        var page = await browser.NewPageAsync();
        await page.SetViewportSizeAsync(375, 667);
        await page.GotoAsync("/next");

        var buy = page.Locator("#nqBuy");
        await Assertions.Expect(buy).ToBeVisibleAsync(new() { Timeout = 30_000 });

        var box = await buy.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box.Y + box.Height <= 667,
            $"Der Kaufen-Knopf endet bei {box.Y + box.Height} px und liegt damit unter der Bildschirmkante des iPhone SE.");
        await browser.ShotAsync(page, "next-quickbuy-se");
    }

    private async Task<IPage> OpenAsync()
    {
        var page = await browser.NewPageAsync();
        await page.SetViewportSizeAsync(390, 844);
        await page.GotoAsync("/next");
        return page;
    }

    private static async Task<decimal> PriceOfAsync(ILocator row) =>
        decimal.Parse(await row.GetAttributeAsync("data-nq-price") ?? "0", CultureInfo.InvariantCulture);

    private static async Task<int> QuantityOfAsync(ILocator row) =>
        int.Parse(await row.Locator("[data-nq-input]").InputValueAsync(), CultureInfo.InvariantCulture);
}
