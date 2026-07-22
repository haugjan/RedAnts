using System.Text;
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
    public async Task<IActionResult> ExportCsv([FromQuery] string? referenz)
    {
        if (string.IsNullOrWhiteSpace(referenz))
            return BadRequest("Referenz fehlt.");

        var cards = await memberCards.GetByReferenceAsync(referenz);

        var baseUrl = publicUrl.Resolve();
        var sb = new StringBuilder();
        sb.Append("Name;Vorname;Geburtsdatum;Link\r\n");
        foreach (var card in cards)
        {
            var birthday = card.Birthday?.ToString("dd.MM.yyyy") ?? "";
            var url = card.Uuid != Guid.Empty
                ? $"{baseUrl}/ticket/{tokens.Create(TicketType.MemberCard, card.Uuid, card.SeasonId)}"
                : "";

            sb.Append(Csv(card.LastName)).Append(';')
              .Append(Csv(card.FirstName)).Append(';')
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
}
