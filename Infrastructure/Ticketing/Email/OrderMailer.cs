using System.Net;
using Microsoft.Extensions.Logging;
using RedAnts.Features.Ticketing.Email;
using RedAnts.Features.Ticketing.Tickets;
using RedAnts.Infrastructure.Shared;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class OrderMailer(
    IEmailSender email,
    ITicketTokens tokens,
    IQrCodeRenderer qr,
    ILogger<OrderMailer> logger) : IOrderMailer
{
    private const string Accent = "#C8102E";

    public async Task<bool> SendTicketsAsync(OrderMailModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = $"Deine Tickets – Bestellung {model.OrderNumber}";
            var greeting = string.IsNullOrWhiteSpace(model.ToName) ? "Hallo," : $"Hallo {model.ToName},";
            var body = BuildBody(model);
            var details = $"Bestellung {model.OrderNumber}\nTotal: CHF {model.Total:N2}";
            var note = "Zeige den QR-Code am Eingang, auf dem Handy oder ausgedruckt. Fragen? Antworte einfach auf diese E-Mail.";
            var html = EmailLayout.Render(subject, body, greeting, details, note);

            var result = await email.SendAsync(model.ToEmail, model.ToName, subject, html, cancellationToken);
            if (!result.Success)
                logger.LogWarning("Ticket e-mail to {Recipient} failed: {Error}", model.ToEmail, result.Error);
            return result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ticket e-mail to {Recipient} threw.", model.ToEmail);
            return false;
        }
    }

    private string BuildBody(OrderMailModel model)
    {
        var intro = "<p style=\"margin:0 0 20px;\">Vielen Dank für deinen Kauf. Deine Tickets sind unten bereit, jedes mit eigenem QR-Code für den Einlass.</p>";
        var blocks = string.Concat(model.Tickets.Select(t => TicketBlock(model.BaseUrl, t)));
        return intro + blocks;
    }

    private string TicketBlock(string baseUrl, OrderMailTicket ticket)
    {
        var token = tokens.Create(ticket.Type, ticket.Uuid, ticket.ScopeId);
        var url = $"{baseUrl}/ticket/{token}";
        var qrPng = qr.RenderPngDataUri(url);
        var reference = ticket.Uuid.ToString("N")[..8].ToUpperInvariant();
        var eventName = WebUtility.HtmlEncode(ticket.EventName);
        var category = WebUtility.HtmlEncode(ticket.CategoryName);

        return
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:0 0 18px;border:1px solid #e5e7eb;border-radius:10px;overflow:hidden;\">" +
                $"<tr><td style=\"background:{Accent};padding:12px 18px;font-family:'Oswald',Arial,Helvetica,sans-serif;color:#ffffff;font-size:18px;font-weight:700;text-transform:uppercase;letter-spacing:0.04em;\">{eventName}</td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:18px 18px 6px;\"><img src=\"{qrPng}\" alt=\"Ticket QR\" width=\"200\" height=\"200\" style=\"display:block;border:1px solid #eeeeee;border-radius:8px;\"></td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:0 18px 4px;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#666666;font-size:13px;\">{category} &middot; Ticket-Nr. {reference}</td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:10px 18px 20px;\"><a href=\"{url}\" style=\"display:inline-block;background:{Accent};color:#ffffff;text-decoration:none;font-family:'Oswald',Arial,Helvetica,sans-serif;font-weight:600;text-transform:uppercase;letter-spacing:0.04em;font-size:14px;padding:11px 22px;border-radius:6px;\">Online-Ticket öffnen</a></td></tr>" +
            "</table>";
    }
}
