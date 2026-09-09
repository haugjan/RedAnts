using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Helpers;

namespace RedAnts.Ticketing.Features.Public;

public sealed class WarmupController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IDataProtectionProvider dataProtection,
    ISeasons seasons,
    IEvents events,
    IVenues venues,
    GetHelpers.Handler helpers,
    IContentUrls contentUrls) : Controller
{
    private static readonly string[] CorePaths =
        ["/", "/ticketing/", "/seasons/", "/next", "/next/embed", "/scan/login", "/scanner-test", "/cart", "/umbraco"];

    [HttpGet("/warmup")]
    public async Task<IActionResult> Warmup()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var paths = CorePaths.ToList();

        foreach (var url in await Task.WhenAll(FirstSeasonUrlAsync(), FirstEventUrlAsync()))
            if (url is not null) paths.Add(url);

        var cookies = new List<string>();
        if (!string.IsNullOrEmpty(configuration["BasicAuth:Password"]))
            cookies.Add($"RedAnts.Gate={dataProtection.CreateProtector("RedAnts.SiteGate.v1").Protect("ok")}");

        var helperCookie = await HelperCookieAsync();
        if (helperCookie is not null)
        {
            cookies.Add(helperCookie);
            paths.Add("/scan");
        }
        var cookieHeader = cookies.Count == 0 ? null : string.Join("; ", cookies);

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

        var scannerData = WarmScannerDataAsync();
        var lines = (await Task.WhenAll(paths.Select(FetchAsync))).Append(await scannerData);
        return Content("warmup\n" + string.Join('\n', lines) + '\n', "text/plain; charset=utf-8");
    }

    private async Task<string?> HelperCookieAsync()
    {
        foreach (var season in await seasons.GetPublicOpenAsync())
        {
            var helper = (await helpers.HandleAsync(new GetHelpers.Query(season.Id))).FirstOrDefault(h => h.Active);
            if (helper is not null)
                return $"{HelperSessionCookie.Name}={HelperSessionCookie.Protect(dataProtection, helper.Id)}";
        }
        return null;
    }

    private async Task<string> WarmScannerDataAsync()
    {
        try
        {
            await Task.WhenAll(venues.GetAllAsync(), events.GetUpcomingForScanningAsync());
            return "scanner data -> ok";
        }
        catch (Exception ex)
        {
            return $"scanner data -> error {ex.GetType().Name}";
        }
    }

    private async Task<string?> FirstSeasonUrlAsync()
    {
        var season = (await seasons.GetPublicOpenAsync()).FirstOrDefault();
        return season is null ? null : contentUrls.GetUrl(season.Id);
    }

    private async Task<string?> FirstEventUrlAsync()
    {
        var ev = (await events.GetPublicOpenAsync())
            .OrderBy(e => e.Date).ThenBy(e => e.StartTime).FirstOrDefault();
        return ev is null ? null : contentUrls.GetUrl(ev.Id);
    }
}
