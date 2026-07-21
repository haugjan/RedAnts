using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Features.Ticketing.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class MemberExportController(IScopeProvider scopeProvider, ITicketTokens tokens, IPublicBaseUrl publicUrl) : Controller
{
    [HttpGet("/admin/mitglieder/referenzen")]
    public async Task<IActionResult> References()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<RefRow>(
            "SELECT DISTINCT Reference FROM MembershipCards " +
            "WHERE Reference IS NOT NULL AND Reference <> '' ORDER BY Reference");
        return Json(rows.Select(r => r.Reference).ToList());
    }

    [HttpGet("/admin/mitglieder/export.csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] string? referenz)
    {
        if (string.IsNullOrWhiteSpace(referenz))
            return BadRequest("Referenz fehlt.");

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<Row>(
            "SELECT Uuid, SeasonId, FirstName, LastName, Birthday FROM MembershipCards " +
            "WHERE Reference = @0 ORDER BY LastName, FirstName", referenz);

        var baseUrl = publicUrl.Resolve(Request);
        var sb = new StringBuilder();
        sb.Append("Name;Vorname;Geburtsdatum;Link\r\n");
        foreach (var r in rows)
        {
            var birthday = r.Birthday?.ToString("dd.MM.yyyy") ?? "";
            var url = Guid.TryParse(r.Uuid, out var uuid)
                ? $"{baseUrl}/ticket/{tokens.Create(TicketType.MemberCard, uuid, r.SeasonId)}"
                : "";

            sb.Append(Csv(r.LastName)).Append(';')
              .Append(Csv(r.FirstName)).Append(';')
              .Append(Csv(birthday)).Append(';')
              .Append(Csv(url)).Append("\r\n");
        }

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString());
        var safeRef = new string(referenz.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
        return File(bytes, "text/csv; charset=utf-8", $"mitglieder-{safeRef}.csv");
    }

    private static string Csv(string? value)
    {
        var s = Neutralize(value ?? "");
        if (s.Contains(';') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private static string Neutralize(string value) =>
        value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
            ? "'" + value
            : value;

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
