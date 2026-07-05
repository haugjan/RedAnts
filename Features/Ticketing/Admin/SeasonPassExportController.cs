using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class SeasonPassExportController(
    ISeasonPassAdminReport report,
    ITicketTokens tokens) : Controller
{
    [HttpGet("/admin/saisonkarten/season/{seasonId:int}/passes.csv")]
    public async Task<IActionResult> Export(int seasonId)
    {
        var passes = await report.GetBySeasonAsync(seasonId);

        var sb = new StringBuilder();
        sb.Append("Karten-Nr;Käufer;Kategorie;Link\r\n");
        foreach (var p in passes)
        {
            var link = $"{Request.Scheme}://{Request.Host}/ticket/{tokens.Create(TicketType.SeasonPass, p.Uuid, seasonId)}";
            sb.Append(ShortCode(p.Uuid)).Append(';')
              .Append(CsvField(p.BuyerName ?? "")).Append(';')
              .Append(CsvField(p.Category.DisplayName())).Append(';')
              .Append(link).Append("\r\n");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"saisonkarten-{seasonId}.csv");
    }

    private static string ShortCode(Guid uuid) => uuid.ToString("N")[..8].ToUpperInvariant();

    private static string CsvField(string value)
    {
        var s = Neutralize(value);
        return s.IndexOfAny([';', '"', '\r', '\n']) >= 0
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;
    }

    private static string Neutralize(string value) =>
        value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
            ? "'" + value
            : value;
}
