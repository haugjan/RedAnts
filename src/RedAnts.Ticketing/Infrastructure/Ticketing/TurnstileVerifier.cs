using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedAnts.Features.Ticketing.Cart;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing;

public sealed class TurnstileVerifier(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<TurnstileVerifier> logger) : ICaptchaVerifier
{
    private const string Endpoint = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public bool Enabled => !string.IsNullOrWhiteSpace(config["Turnstile:SecretKey"]);

    public string? SiteKey => config["Turnstile:SiteKey"];

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var secret = config["Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret)) return true;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var form = new Dictionary<string, string> { ["secret"] = secret, ["response"] = token };
        if (!string.IsNullOrWhiteSpace(remoteIp)) form["remoteip"] = remoteIp;

        try
        {
            var client = httpClientFactory.CreateClient("turnstile");
            using var response = await client.PostAsync(Endpoint, new FormUrlEncodedContent(form), cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>(cancellationToken);
            if (result?.Success == true) return true;

            logger.LogWarning("Turnstile verification failed: {Errors}", string.Join(",", result?.ErrorCodes ?? []));
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Turnstile verification threw.");
            return false;
        }
    }

    private sealed record SiteVerifyResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
}

public sealed class TurnstileVerifierComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient("turnstile");
        builder.Services.AddScoped<ICaptchaVerifier, TurnstileVerifier>();
    }
}
