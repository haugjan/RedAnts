namespace RedAnts.Ticketing.Features.Checkout;

public sealed record CheckoutSettings(bool PayrexxEnabled, string? TurnstileSiteKey);

public static class GetCheckoutSettings
{
    public sealed record Query;

    public sealed class Handler(IPayrexxGateway payrexx, ICaptchaVerifier captcha)
    {
        public Task<CheckoutSettings> HandleAsync(Query query) =>
            Task.FromResult(new CheckoutSettings(payrexx.Enabled, captcha.Enabled ? captcha.SiteKey : null));
    }
}
