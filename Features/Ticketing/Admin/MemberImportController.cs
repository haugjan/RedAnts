using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>Serves the member-import CSV template so an admin sees the expected format
/// (Name; Vorname; Geburtsdatum).</summary>
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class MemberImportController : Controller
{
    // GET /admin/mitglieder/beispiel.csv — download the template.
    [HttpGet("/admin/mitglieder/beispiel.csv")]
    public IActionResult SampleCsv() =>
        File(MemberCsv.SampleBytes(), "text/csv; charset=utf-8", "mitglieder-vorlage.csv");
}
