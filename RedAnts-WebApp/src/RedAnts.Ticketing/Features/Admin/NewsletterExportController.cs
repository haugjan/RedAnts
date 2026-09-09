using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Features.Ports;
using Umbraco.Cms.Core;

namespace RedAnts.Ticketing.Features.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class NewsletterExportController(INewsletterSignups signups) : Controller
{
    [HttpGet("/admin/newsletter/export.csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var pending = await signups.GetPendingAsync();
        var stamp = SwissTime.Now.ToString("yyyyMMdd");
        return File(NewsletterFairgateCsv.Build(pending), "text/csv; charset=utf-8", $"newsletter-fairgate-{stamp}.csv");
    }
}
