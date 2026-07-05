using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Features.Ticketing.Admin;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class MemberImportController : Controller
{
    [HttpGet("/admin/mitglieder/beispiel.csv")]
    public IActionResult SampleCsv() =>
        File(MemberCsv.SampleBytes(), "text/csv; charset=utf-8", "mitglieder-vorlage.csv");
}
