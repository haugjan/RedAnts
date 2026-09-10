using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Catalog.Shop;
using RedAnts.Ticketing.Features.Helpers;

namespace RedAnts.Ticketing.Features.Public;

public sealed class WarmupController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IDataProtectionProvider dataProtection,
    GetSeasonPassOffers.Handler seasonPassOffers,
    GetNextEvent.Handler nextEvent,
    GetTicketingHome.Handler ticketingHome,
    GetHelpers.Handler helpers) : Controller
{
    private static readonly TimeSpan PageTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan TotalBudget = TimeSpan.FromSeconds(100);

    private static readonly string[] CorePaths =
        ["/", "/ticketing/", "/seasons/", "/next", "/next/embed", "/scan/login", "/scanner-test", "/cart", "/umbraco"];

    [HttpGet("/warmup")]
    public async Task<IActionResult> Warmup()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var paths = CorePaths.ToList();

        var openSeasons = await seasonPassOffers.HandleAsync(new GetSeasonPassOffers.Query());
        if (openSeasons.FirstOrDefault()?.Url is { } seasonUrl) paths.Add(seasonUrl);
        if ((await nextEvent.HandleAsync(new GetNextEvent.Query()))?.Url is { } eventUrl) paths.Add(eventUrl);

        var cookies = new List<string>();
        if (!string.IsNullOrEmpty(configuration["BasicAuth:Password"]))
            cookies.Add($"RedAnts.Gate={dataProtection.CreateProtector("RedAnts.SiteGate.v1").Protect("ok")}");

        var helperCookie = await HelperCookieAsync(openSeasons.Select(s => s.SeasonId));
        if (helperCookie is not null)
        {
            cookies.Add(helperCookie);
            paths.Add("/scan");
        }
        var cookieHeader = cookies.Count == 0 ? null : string.Join("; ", cookies);

        var scannerData = await WarmScannerDataAsync();

        var deadline = DateTimeOffset.UtcNow.Add(TotalBudget);
        var client = httpClientFactory.CreateClient();
        client.Timeout = PageTimeout;

        async Task<string> FetchAsync(string path)
        {
            var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : baseUrl + path;
            Exception? failure = null;
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    if (cookieHeader is not null) req.Headers.Add("Cookie", cookieHeader);
                    using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead);
                    return $"{path} -> {(int)resp.StatusCode}";
                }
                catch (Exception ex)
                {
                    failure = ex;
                    if (ex is not TaskCanceledException || DateTimeOffset.UtcNow >= deadline) break;
                }
            }
            return $"{path} -> error {failure!.GetType().Name}";
        }

        var lines = new List<string>();
        foreach (var path in paths)
            lines.Add(DateTimeOffset.UtcNow < deadline ? await FetchAsync(path) : $"{path} -> not reached");
        lines.Add(scannerData);
        return Content("warmup\n" + string.Join('\n', lines) + '\n', "text/plain; charset=utf-8");
    }

    private async Task<string?> HelperCookieAsync(IEnumerable<int> seasonIds)
    {
        foreach (var seasonId in seasonIds)
        {
            var helper = (await helpers.HandleAsync(new GetHelpers.Query(seasonId))).FirstOrDefault(h => h.Active);
            if (helper is not null)
                return $"{HelperSessionCookie.Name}={HelperSessionCookie.Protect(dataProtection, helper.Id)}";
        }
        return null;
    }

    private async Task<string> WarmScannerDataAsync()
    {
        try
        {
            await ticketingHome.HandleAsync(new GetTicketingHome.Query());
            return "scanner data -> ok";
        }
        catch (Exception ex)
        {
            return $"scanner data -> error {ex.GetType().Name}";
        }
    }
}
