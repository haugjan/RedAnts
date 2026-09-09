namespace RedAnts.Ticketing.Features.Checkout;

public static class VerifyCaptcha
{
    public sealed record Query(string? Response, string? RemoteIp);

    public sealed class Handler(ICaptchaVerifier captcha)
    {
        public Task<bool> HandleAsync(Query query) => captcha.VerifyAsync(query.Response, query.RemoteIp);
    }
}
