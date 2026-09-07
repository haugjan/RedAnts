using Microsoft.Playwright;

namespace RedAnts.BrowserTests;

[Collection(BrowserCollection.Name)]
public sealed class CheckoutShould(BrowserFixture browser)
{
    private const string EventPath = "/seasons/saison-202627/red-ants-vs-fb-riders-dbr/";

    [E2EFact]
    public async Task PlaceAnOrderFromTheEventPage()
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync(EventPath);
        var addForm = page.Locator("form.evt-add-form").First;
        await Assertions.Expect(addForm).ToBeVisibleAsync();
        await addForm.Locator("button[type=submit]").ClickAsync();
        await Assertions.Expect(addForm.Locator("button[type=submit]")).ToContainTextAsync("Hinzugefügt");

        await page.GotoAsync("/cart");
        await browser.ShotAsync(page, "checkout-cart");
        await Assertions.Expect(page.Locator("body")).Not.ToContainTextAsync("Warenkorb ist leer");

        await page.GotoAsync("/checkout");
        if (await page.Locator(".cf-turnstile").CountAsync() > 0)
            return;

        await page.FillAsync("input[name=FirstName]", "Agent");
        await page.FillAsync("input[name=LastName]", "Browser");
        await page.FillAsync("input[name=Street]", "Teststrasse 1");
        await page.FillAsync("input[name=PostalCode]", "8400");
        await page.FillAsync("input[name=City]", "Winterthur");
        await page.FillAsync("input[name=Email]", "agent-checkout@redants.ch");
        await page.CheckAsync("#acceptPrivacy");
        await browser.ShotAsync(page, "checkout-address");
        await page.ClickAsync("button[type=submit].btn-danger");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await browser.ShotAsync(page, "checkout-result");

        var url = page.Url;
        var completed = url.Contains("/checkout/confirmation", StringComparison.OrdinalIgnoreCase);
        var atPayrexx = url.Contains("payrexx", StringComparison.OrdinalIgnoreCase);
        Assert.True(completed || atPayrexx, $"Checkout ended at {url}");
        if (completed)
            await Assertions.Expect(page.Locator("body")).ToContainTextAsync("Bestellnummer");
    }
}
