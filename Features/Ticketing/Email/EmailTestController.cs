using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Tickets;
using RedAnts.Infrastructure.Shared;

namespace RedAnts.Features.Ticketing.Email;

public sealed class EmailTestController(
    IEmailSender email,
    IWebHostEnvironment environment,
    IIssuedTicketReader tickets,
    ITicketTokens tokens,
    IQrCodeRenderer qr,
    IEvents events,
    ISeasons seasons,
    IPublicBaseUrl publicUrl,
    IOrderMailer orderMailer) : Controller
{
    [HttpGet("/dev/ticket-mail-preview")]
    public async Task<IActionResult> TicketMailPreview(string? to = null)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var model = new OrderMailModel(
            "2026-000123", to ?? "max.muster@example.com", "Max Muster", 45m,
            publicUrl.Resolve(Request),
            [
                new(TicketType.EventTicket, Guid.Empty, 0, "Red Ants vs. UHC Beispielgegner", "Erwachsen"),
                new(TicketType.SeasonPass, Guid.Empty, 0, "Saison 2026/27", "Erwachsen")
            ]);
        if (!string.IsNullOrWhiteSpace(to))
        {
            var ok = await orderMailer.SendTicketsAsync(model);
            return Content(ok ? $"Testmail an {to} gesendet." : "Versand fehlgeschlagen (siehe Logs).");
        }
        return Content(await orderMailer.RenderAsync(model), "text/html");
    }

    [HttpGet("/dev/test-mail")]
    public async Task<IActionResult> Send(string? to, Guid? uuid)
    {
        if (!environment.IsDevelopment()) return NotFound();
        if (string.IsNullOrWhiteSpace(to)) return BadRequest("Query parameter ?to=<email> is required.");

        string subject;
        string html;

        if (uuid is Guid ticketId && await tickets.FindAsync(ticketId) is { } issued)
        {
            var token = tokens.Create(issued.Type, issued.Uuid, issued.ScopeId);
            var url = $"{publicUrl.Resolve(Request)}/ticket/{token}";
            var qrPng = qr.RenderPngDataUri(url);
            var scopeName = issued.Type == TicketType.EventTicket
                ? (await events.FindByIdAsync(issued.ScopeId))?.Name ?? "Anlass"
                : (await seasons.FindByIdAsync(issued.ScopeId))?.Name ?? "Saison";

            subject = $"Dein Ticket – {scopeName}";
            var body =
                $"Hier ist dein Ticket für <strong>{scopeName}</strong>.\nZeige den QR-Code am Eingang.\n\n" +
                $"<div style=\"text-align:center;margin:16px 0;\"><img src=\"{qrPng}\" alt=\"Ticket QR\" width=\"220\" height=\"220\" style=\"border:1px solid #eee;border-radius:8px;\"></div>\n" +
                $"Web-Ticket: <a href=\"{url}\">{url}</a>";
            html = EmailLayout.Render(subject, body, greeting: "Hallo,",
                note: "Testmail aus der RedAnts-Entwicklungsumgebung.");
        }
        else
        {
            subject = "Brevo-Testmail – Red Ants";
            html = EmailLayout.Render(subject,
                "Diese Testmail bestätigt, dass der Brevo-Versand aus RedAnts funktioniert.",
                greeting: "Hallo,", note: "Testmail aus der RedAnts-Entwicklungsumgebung.");
        }

        var result = await email.SendAsync(to, null, subject, html);
        return result.Success
            ? Content($"OK – Mail an {to} gesendet.")
            : StatusCode(502, $"Versand fehlgeschlagen: {result.Error}");
    }
}
