using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class NewsletterExportController(INewsletterSignups signups) : Controller
{
    [HttpGet("/admin/newsletter/export.csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var pending = await signups.GetPendingAsync();
        var stamp = DateTime.Now.ToString("yyyyMMdd");
        return File(NewsletterFairgateCsv.Build(pending), "text/csv; charset=utf-8", $"newsletter-fairgate-{stamp}.csv");
    }
}
