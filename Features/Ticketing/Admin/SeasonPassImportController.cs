using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Features.Ticketing.Admin;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class SeasonPassImportController : Controller
{
    [HttpGet("/admin/saisonkarten/beispiel.csv")]
    public IActionResult SampleCsv() =>
        File(SeasonPassCsv.SampleBytes(), "text/csv; charset=utf-8", "saisonkarten-vorlage.csv");
}
