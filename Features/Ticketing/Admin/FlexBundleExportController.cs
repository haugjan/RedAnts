using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>Downloads a whole Flexticket bundle as a two-column CSV: the ticket short code and its
/// ticket-token link (the public <c>/ticket/{token}</c> URL). Behind the backoffice auth scheme, like
/// the ticketing admin dashboard. The token is minted per ticket via S3's <see cref="ITicketTokens"/>.</summary>
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class FlexBundleExportController(IFlexBundleTickets bundleTickets, ITicketTokens tokens) : Controller
{
    [HttpGet("/admin/flextickets/bundle/{bundleId:int}/tickets.csv")]
    public async Task<IActionResult> Export(int bundleId)
    {
        var tickets = await bundleTickets.GetByBundleAsync(bundleId);

        var sb = new StringBuilder();
        sb.Append("Kurzkennung;Link\r\n");
        foreach (var t in tickets)
        {
            var token = tokens.Create(TicketType.SeasonSingle, t.Uuid, t.SeasonId);
            var link = $"{Request.Scheme}://{Request.Host}/ticket/{token}";
            var shortCode = t.Uuid.ToString("N")[..8].ToUpperInvariant();
            sb.Append(shortCode).Append(';').Append(link).Append("\r\n");
        }

        // Prepend a UTF-8 BOM so Excel opens the file with the right encoding.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"flextickets-bundle-{bundleId}.csv");
    }
}
