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
    ITicketTokens tokens) : Controller
{
    [HttpGet("/admin/flextickets/bundle/{bundleId:int}/tickets.csv")]
    public async Task<IActionResult> Export(int bundleId)
    {
        var tickets = await bundleTickets.GetByBundleAsync(bundleId);

        var sb = new StringBuilder();
        sb.Append("Referenz;Kurzkennung;Link\r\n");
        foreach (var t in tickets)
        {
            var link = $"{Request.Scheme}://{Request.Host}/ticket/{tokens.Create(TicketType.SeasonSingle, t.Uuid, t.SeasonId)}";
            sb.Append(CsvField(t.Reference)).Append(';')
              .Append(ShortCode(t.Uuid)).Append(';')
              .Append(link).Append("\r\n");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"flextickets-bundle-{bundleId}.csv");
    }

    private static string ShortCode(Guid uuid) => uuid.ToString("N")[..8].ToUpperInvariant();

    // Quote a value for the semicolon-separated CSV when it contains a separator, quote or newline.
    private static string CsvField(string value) =>
        value.IndexOfAny([';', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
