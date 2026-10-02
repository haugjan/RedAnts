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
        await Assertions.Expect(minus).ToBeDisabledAsync();
        await row.Locator("[data-nq-step='1']").ClickAsync();
        await minus.ClickAsync();

        Assert.Equal(0, await QuantityOfAsync(row));
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

    [E2EFact]
    public async Task HandOverEveryTicketOfAQuickBuy()
    {
        var page = await browser.NewPageAsync();
        await page.SetViewportSizeAsync(375, 667);
        await page.GotoAsync("/next");

        var rows = page.Locator("[data-nq-row]");
        await Assertions.Expect(rows.First).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await rows.First.Locator("[data-nq-step='1']").ClickAsync();
        await rows.First.Locator("[data-nq-step='1']").ClickAsync();
        await rows.Nth(1).Locator("[data-nq-step='1']").ClickAsync();

        await page.FillAsync("#nqEmail", "agent-quickbuy@redants.ch");
        await page.CheckAsync("#nqPrivacy");
        await WaitForCaptchaAsync(page);
        await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#nqBuy"),
            response => response.Url.Contains("/next/buy", StringComparison.OrdinalIgnoreCase));
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var rejected = page.Locator(".nq-error");
        if (await rejected.CountAsync() > 0)
            Assert.Fail($"Der Kauf wurde abgewiesen: {await rejected.InnerTextAsync()}");

        if (page.Url.Contains("payrexx", StringComparison.OrdinalIgnoreCase)) return;
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.Locator(".ra-ticket-cell")).ToHaveCountAsync(3);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(browser.ScreenshotDirectory, "next-quickbuy-done-fold.png")
        });
        await browser.ShotAsync(page, "next-quickbuy-done");

        var firstTicket = await page.Locator(".ra-ticket-cell").First.BoundingBoxAsync();
        Assert.NotNull(firstTicket);
        Assert.True(firstTicket.Y < 607,
            $"Das erste Ticket beginnt erst bei {firstTicket.Y} px, auf dem iPhone SE sieht man also nicht mehr, dass unten Tickets folgen.");
    }

    private static async Task WaitForCaptchaAsync(IPage page)
    {
        if (await page.Locator(".cf-turnstile").CountAsync() == 0) return;
        await page.WaitForFunctionAsync(
            @"() => { const token = document.querySelector('input[name=""cf-turnstile-response""]'); return !!token && token.value.length > 0; }",
            null,
            new PageWaitForFunctionOptions { Timeout = 20_000 });
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
