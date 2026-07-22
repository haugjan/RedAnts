using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Public;

public sealed class WarmupController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IDataProtectionProvider dataProtection,
    ISeasons seasons,
    IContentUrls contentUrls) : Controller
{
    private static readonly string[] CorePaths =
        ["/", "/ticketing/", "/seasons/", "/next", "/next/embed", "/scanner-test", "/umbraco"];

    [HttpGet("/warmup")]
    public async Task<IActionResult> Warmup()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var paths = CorePaths.ToList();

        var seasonUrl = await FirstSeasonUrlAsync();
        if (seasonUrl is not null) paths.Add(seasonUrl);

        string? gateCookie = null;
        if (!string.IsNullOrEmpty(configuration["BasicAuth:Password"]))
        {
            var token = dataProtection.CreateProtector("RedAnts.SiteGate.v1").Protect("ok");
            gateCookie = $"RedAnts.Gate={token}";
        }

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(45);

        var report = new StringBuilder("warmup\n");
        foreach (var path in paths)
        {
            var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : baseUrl + path;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (gateCookie is not null) request.Headers.Add("Cookie", gateCookie);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                report.Append(path).Append(" -> ").Append((int)response.StatusCode).Append('\n');
            }
            catch (Exception ex)
            {
                report.Append(path).Append(" -> error ").Append(ex.GetType().Name).Append('\n');
            }
        }

        return Content(report.ToString(), "text/plain; charset=utf-8");
    }

    private async Task<string?> FirstSeasonUrlAsync()
    {
        var season = (await seasons.GetPublicOpenAsync()).FirstOrDefault();
        return season is null ? null : contentUrls.GetUrl(season.Id);
    }
}
