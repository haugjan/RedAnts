using System.Text;
using Microsoft.AspNetCore.Mvc;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>
/// Exports the member cards of one import reference as a CSV for a mail merge (Serienbrief): the same
/// columns as the member import (Name; Vorname; Geburtsdatum) plus a <c>QrUrl</c> column holding the
/// absolute ticket URL that the member's QR encodes (a mail-merge tool renders the QR from that URL
/// text). Self-contained: reads the <c>MembershipCards</c> table directly and mints the token via S3's
/// <see cref="ITicketTokens"/>.
///
/// SECURITY: this returns valid admission tokens plus member PII. During the live test it is covered by
/// the global HTTP Basic gate (Program.cs); add proper backoffice authorization before the site is
/// opened to the public.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class MemberExportController(IScopeProvider scopeProvider, ITicketTokens tokens) : Controller
{
    // GET /admin/mitglieder/referenzen — distinct references, to fill the export dialog's dropdown.
    [HttpGet("/admin/mitglieder/referenzen")]
    public async Task<IActionResult> References()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<RefRow>(
            "SELECT DISTINCT Reference FROM MembershipCards " +
            "WHERE Reference IS NOT NULL AND Reference <> '' ORDER BY Reference");
        return Json(rows.Select(r => r.Reference).ToList());
    }

    // GET /admin/mitglieder/export.csv?referenz=... — member cards of that reference + a QR-url column.
    [HttpGet("/admin/mitglieder/export.csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] string? referenz)
    {
        if (string.IsNullOrWhiteSpace(referenz))
            return BadRequest("Referenz fehlt.");

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<Row>(
            "SELECT Uuid, SeasonId, FirstName, LastName, Birthday FROM MembershipCards " +
            "WHERE Reference = @0 ORDER BY LastName, FirstName", referenz);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();
        sb.Append("Name;Vorname;Geburtsdatum;QrUrl\n");
        foreach (var r in rows)
        {
            var birthday = r.Birthday?.ToString("dd.MM.yyyy") ?? "";
            var url = Guid.TryParse(r.Uuid, out var uuid)
                ? $"{baseUrl}/ticket/{tokens.Create(TicketType.MemberCard, uuid, r.SeasonId)}"
                : "";

            sb.Append(Csv(r.LastName)).Append(';')
              .Append(Csv(r.FirstName)).Append(';')
              .Append(Csv(birthday)).Append(';')
              .Append(Csv(url)).Append('\n');
        }

        // UTF-8 with BOM so Excel renders umlauts correctly (matches the import template).
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString());
        var safeRef = new string(referenz.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
        return File(bytes, "text/csv; charset=utf-8", $"mitglieder-{safeRef}.csv");
    }

    /// <summary>Quote a CSV field only when it contains the delimiter, a quote or a line break.</summary>
    private static string Csv(string? value)
    {
        var s = value ?? "";
        if (s.Contains(';') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private sealed class RefRow
    {
        public string Reference { get; set; } = "";
    }

    private sealed class Row
    {
        public string Uuid { get; set; } = "";
        public int SeasonId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? Birthday { get; set; }
    }
}
