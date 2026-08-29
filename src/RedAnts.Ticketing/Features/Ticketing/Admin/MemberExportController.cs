using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class MemberExportController(IMemberCards memberCards, ITicketTokens tokens, IPublicBaseUrl publicUrl) : Controller
{
    [HttpGet("/admin/members/references")]
    public async Task<IActionResult> References()
        => Json(await memberCards.GetReferencesAsync());

    [HttpGet("/admin/members/export.csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] string? bundles)
    {
        var selected = (bundles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (selected.Length == 0)
            return BadRequest("Bundle fehlt.");

        var rows = new List<TicketExportRow>();
        foreach (var referenz in selected)
        {
            foreach (var card in await memberCards.GetByReferenceAsync(referenz))
            {
                var a = card.Address;
                var holder = CardHolder.Create(
                    a.Company is { Length: > 0 } ? BuyerType.Company : BuyerType.Private,
                    a.Salutation, a.Company, card.FirstName, card.LastName, card.Birthday, card.Email,
                    a.Street, a.AddressLine2, a.PostalCode, a.City, a.Country, a.Phone);
                var link = card.Uuid != Guid.Empty ? publicUrl.TicketUrl(tokens.CreateShort(card.Uuid)) : "";
                var cardNo = card.Uuid != Guid.Empty ? card.Uuid.ToString("N")[..8].ToUpperInvariant() : "";
                rows.Add(new TicketExportRow(cardNo, referenz, card.Category.DisplayName(), holder, card.Admissions, link));
            }
        }

        var suffix = selected.Length == 1
            ? new string(selected[0].Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray())
            : "alle";
        return File(TicketExportCsv.Build(rows), "text/csv; charset=utf-8", $"mitglieder-{suffix}.csv");
    }
}
