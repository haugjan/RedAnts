using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Checkout;

public class CheckoutQueryTests
{
    [Fact]
    public async Task Cart_summary_mirrors_the_session_cart()
    {
        var carts = new InMemoryCart();
        var cart = Cart.Empty();
        cart.AddEventTickets(10, "Red Ants vs. Gegner", 1, "Erwachsen", "Erw", 20m, 2);
        cart.AddSeasonPasses(3, "Saison 2026/27", 7, "Erwachsen", "Erw", 300m, 1, [new CartAddOn(9, "Karte", 5m, 3, "Saison 2026/27", true)]);
        cart.AddOrderAddOns([new CartAddOn(11, "Livestream", 30m, 3, "Saison 2026/27")]);
        carts.Save(cart);

        var summary = await new GetCart.Handler(carts).HandleAsync(new GetCart.Query());

        Assert.Equal(2, summary.Items.Count);
        Assert.Equal(cart.Items[0].Key, summary.Items[0].Key);
        Assert.Equal(40m, summary.Items[0].LineTotal);
        Assert.Equal("Karte", Assert.Single(summary.Items[1].AddOns).Label);
        Assert.Equal("Livestream", Assert.Single(summary.OrderAddOns).Label);
        Assert.Equal(cart.TotalAmount, summary.TotalAmount);
        Assert.Equal(4, summary.TotalQuantity);
        Assert.False(summary.IsEmpty);
        Assert.False(summary.QualifiesForExpress);
        Assert.True(summary.RequiresMobileNumber);
    }

    [Fact]
    public async Task Empty_session_yields_an_empty_summary()
    {
        var summary = await new GetCart.Handler(new InMemoryCart()).HandleAsync(new GetCart.Query());

        Assert.True(summary.IsEmpty);
        Assert.Empty(summary.Items);
        Assert.Equal(0m, summary.TotalAmount);
    }

    [Fact]
    public async Task Checkout_settings_expose_the_site_key_only_while_the_captcha_is_enabled()
    {
        var payrexx = new StubPayrexx { Enabled = true };
        var captcha = new StubCaptcha { Enabled = false, SiteKey = "site-key" };

        var settings = await new GetCheckoutSettings.Handler(payrexx, captcha).HandleAsync(new GetCheckoutSettings.Query());
        Assert.True(settings.PayrexxEnabled);
        Assert.Null(settings.TurnstileSiteKey);

        captcha.Enabled = true;
        settings = await new GetCheckoutSettings.Handler(payrexx, captcha).HandleAsync(new GetCheckoutSettings.Query());
        Assert.Equal("site-key", settings.TurnstileSiteKey);
    }

    [Fact]
    public async Task Captcha_verification_forwards_response_and_remote_ip()
    {
        var captcha = new StubCaptcha { Passes = true };

        var passed = await new VerifyCaptcha.Handler(captcha).HandleAsync(new VerifyCaptcha.Query("token-1", "10.0.0.1"));

        Assert.True(passed);
        Assert.Equal(("token-1", "10.0.0.1"), captcha.LastVerification);
    }

    [Fact]
    public async Task Checkout_order_is_found_by_token_or_by_order_number()
    {
        var orders = new StubCheckoutOrders();
        orders.IdsByNumber["T-007"] = 7;
        var handler = new FindCheckoutOrder.Handler(new StubOrderTokens(), orders);

        Assert.Equal(5, await handler.HandleAsync(new FindCheckoutOrder.Query(Token: "tok5")));
        Assert.Equal(7, await handler.HandleAsync(new FindCheckoutOrder.Query(OrderNumber: " T-007 ")));
        Assert.Null(await handler.HandleAsync(new FindCheckoutOrder.Query(Token: "bogus")));
        Assert.Null(await handler.HandleAsync(new FindCheckoutOrder.Query(OrderNumber: "T-999")));
        Assert.Null(await handler.HandleAsync(new FindCheckoutOrder.Query()));
    }
}

internal sealed class StubCaptcha : ICaptchaVerifier
{
    public bool Enabled { get; set; }
    public string? SiteKey { get; set; }
    public bool Passes { get; set; }
    public (string? Token, string? RemoteIp) LastVerification { get; private set; }

    public Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default)
    {
        LastVerification = (token, remoteIp);
        return Task.FromResult(Passes);
    }
}

internal sealed class StubCheckoutOrders : ICheckoutOrderReader
{
    public Dictionary<string, int> IdsByNumber { get; } = new();

    public Task<int?> FindIdByNumberAsync(string orderNumber) =>
        Task.FromResult(IdsByNumber.TryGetValue(orderNumber, out var id) ? id : (int?)null);
}
