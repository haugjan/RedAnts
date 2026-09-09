using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;

namespace RedAnts.Ticketing.Features.Newsletter.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class NewsletterExportController(GetNewsletterSignups.Handler signups) : Controller
{
    [HttpGet("/admin/newsletter/export.csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var pending = await signups.HandleAsync(new GetNewsletterSignups.Query(OnlyPending: true));
        var stamp = SwissTime.Now.ToString("yyyyMMdd");
        return File(NewsletterFairgateCsv.Build(pending), "text/csv; charset=utf-8", $"newsletter-fairgate-{stamp}.csv");
    }
}
