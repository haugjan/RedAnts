using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Public;

public sealed class WarmupController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IDataProtectionProvider dataProtection,
    ISeasons seasons,
    IEvents events,
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

        string? gateCookie = null;
        if (!string.IsNullOrEmpty(configuration["BasicAuth:Password"]))
        {
            var token = dataProtection.CreateProtector("RedAnts.SiteGate.v1").Protect("ok");
            gateCookie = $"RedAnts.Gate={token}";
        }

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        async Task<string> FetchAsync(string path)
        {
            var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : baseUrl + path;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                if (gateCookie is not null) req.Headers.Add("Cookie", gateCookie);
                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead);
                return $"{path} -> {(int)resp.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"{path} -> error {ex.GetType().Name}";
            }
        }

        var lines = await Task.WhenAll(paths.Select(FetchAsync));
        return Content("warmup\n" + string.Join('\n', lines) + '\n', "text/plain; charset=utf-8");
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
