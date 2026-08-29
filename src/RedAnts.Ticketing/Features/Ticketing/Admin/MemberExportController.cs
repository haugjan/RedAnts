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
            return BadRequest("Bundle fehlt.");

        var cards = await memberCards.GetByReferenceAsync(referenz);

        var sb = new StringBuilder();
        sb.Append("Karten-Nr;Kategorie;Anzahl;Firma;Anrede;Name;Vorname;Strasse;Adresszusatz;PLZ;Ort;Land;E-Mail;Telefon;Geburtsdatum;Bundle;Link\r\n");
        foreach (var card in cards)
        {
            var birthday = card.Birthday?.ToString("dd.MM.yyyy") ?? "";
            var url = card.Uuid != Guid.Empty
                ? publicUrl.TicketUrl(tokens.CreateShort(card.Uuid))
                : "";
            var cardNo = card.Uuid != Guid.Empty ? card.Uuid.ToString("N")[..8].ToUpperInvariant() : "";
            var a = card.Address;

            sb.Append(Csv(cardNo)).Append(';')
              .Append(Csv(card.Category.DisplayName())).Append(';')
              .Append(Csv(card.Admissions.ToString())).Append(';')
              .Append(Csv(a.Company)).Append(';')
              .Append(Csv(a.Salutation)).Append(';')
              .Append(Csv(card.LastName)).Append(';')
              .Append(Csv(card.FirstName)).Append(';')
              .Append(Csv(a.Street)).Append(';')
              .Append(Csv(a.AddressLine2)).Append(';')
              .Append(Csv(a.PostalCode)).Append(';')
              .Append(Csv(a.City)).Append(';')
              .Append(Csv(a.Country)).Append(';')
              .Append(Csv(card.Email)).Append(';')
              .Append(Csv(a.Phone)).Append(';')
              .Append(Csv(birthday)).Append(';')
              .Append(Csv(referenz)).Append(';')
              .Append(Csv(url)).Append("\r\n");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
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
