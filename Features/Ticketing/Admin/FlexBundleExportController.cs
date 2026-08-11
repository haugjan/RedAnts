using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class FlexBundleExportController(
    IFlexBundleTickets bundleTickets,
    ITicketTokens tokens,
    IPublicBaseUrl publicUrl) : Controller
{
    [HttpGet("/admin/flex-tickets/bundles.csv")]
    public async Task<IActionResult> Export([FromQuery] string? ids)
    {
        var bundleIds = (ids ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .Where(n => n > 0)
            .Distinct()
            .ToList();

        var tickets = await bundleTickets.GetByBundlesAsync(bundleIds);

        var sb = new StringBuilder();
        sb.Append("Karten-Nr;Bundle;Link\r\n");
        foreach (var t in tickets)
        {
            var link = publicUrl.TicketUrl(tokens.CreateShort(t.Uuid));
            sb.Append(ShortCode(t.Uuid)).Append(';')
              .Append(CsvField(t.Reference)).Append(';')
              .Append(link).Append("\r\n");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var name = bundleIds.Count == 1 ? $"flextickets-bundle-{bundleIds[0]}.csv" : "flextickets-bundles.csv";
        return File(bytes, "text/csv; charset=utf-8", name);
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
