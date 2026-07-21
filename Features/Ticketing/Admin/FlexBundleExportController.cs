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
    [HttpGet("/admin/flex-tickets/bundle/{bundleId:int}/tickets.csv")]
    public async Task<IActionResult> Export(int bundleId)
    {
        var tickets = await bundleTickets.GetByBundleAsync(bundleId);

        var sb = new StringBuilder();
        sb.Append("Referenz;Kurzkennung;Link\r\n");
        foreach (var t in tickets)
        {
            var link = $"{publicUrl.Resolve(Request)}/ticket/{tokens.Create(TicketType.SeasonSingle, t.Uuid, t.SeasonId)}";
            sb.Append(CsvField(t.Reference)).Append(';')
              .Append(ShortCode(t.Uuid)).Append(';')
              .Append(link).Append("\r\n");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"flextickets-bundle-{bundleId}.csv");
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
