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
    IHelpers helpers) : Controller
{
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

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        async Task<string> FetchAsync(string path)
        {
            var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : baseUrl + path;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                if (cookieHeader is not null) req.Headers.Add("Cookie", cookieHeader);
                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead);
                return $"{path} -> {(int)resp.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"{path} -> error {ex.GetType().Name}";
            }
        }

        var lines = (await Task.WhenAll(paths.Select(FetchAsync))).Append(scannerData);
        return Content("warmup\n" + string.Join('\n', lines) + '\n', "text/plain; charset=utf-8");
    }

    private async Task<string?> HelperCookieAsync(IEnumerable<int> seasonIds)
    {
        foreach (var seasonId in seasonIds)
        {
            var helper = (await helpers.GetBySeasonAsync(seasonId)).FirstOrDefault(h => h.Active);
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
