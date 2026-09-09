namespace RedAnts.Ticketing.Features.Checkout;

public interface ICaptchaVerifier
{
    bool Enabled { get; }

    string? SiteKey { get; }

    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default);
}
