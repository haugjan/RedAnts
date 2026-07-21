using System.Net;
using Microsoft.Extensions.Logging;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Email;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Tickets;
using RedAnts.Infrastructure.Shared;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class OrderMailer(
    IEmailSender email,
    ITicketTokens tokens,
    IEvents events,
    ISeasons seasons,
    IWalletPass wallet,
    ILogger<OrderMailer> logger) : IOrderMailer
{
    private const string LogoUrl = "https://redants.ch/uploads/232/admin/website_header/RA_Logo_transparent_300ppi.png";

    public async Task<bool> SendTicketsAsync(OrderMailModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = $"Deine Tickets – Bestellung {model.OrderNumber}";
            var greeting = string.IsNullOrWhiteSpace(model.ToName) ? "Hallo," : $"Hallo {model.ToName},";
            var body = await BuildBodyAsync(model);
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

    private async Task<string> BuildBodyAsync(OrderMailModel model)
    {
        var intro = "<p style=\"margin:0 0 20px;\">Vielen Dank für deinen Kauf. Deine Tickets sind unten bereit, jedes mit eigenem QR-Code für den Einlass.</p>";
        var dates = await ResolveDatesAsync(model.Tickets);
        var total = model.Tickets.Count;
        var blocks = string.Concat(model.Tickets.Select((t, i) =>
            TicketCard(model.BaseUrl, t, dates.GetValueOrDefault((t.Type, t.ScopeId)), i + 1, total)));
        return intro + blocks + AddOnInfoBlock(model.AddOnInfoTexts);
    }

    private static string AddOnInfoBlock(IReadOnlyList<string>? infos)
    {
        if (infos is null || infos.Count == 0) return "";
        var items = string.Concat(infos.Select(info =>
            $"<p style=\"margin:0 0 10px;\">{WebUtility.HtmlEncode(info).Replace("\n", "<br>")}</p>"));
        return "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"max-width:400px;margin:0 auto 32px;background:#f5f5f5;border-left:4px solid #C8102E;border-radius:4px;\">" +
            "<tr><td style=\"padding:16px 18px;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#323232;font-size:14px;line-height:1.6;\">" +
                "<div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;font-weight:700;text-transform:uppercase;letter-spacing:0.04em;color:#101010;font-size:14px;margin:0 0 8px;\">Zu deinen Zusatzoptionen</div>" +
                items +
            "</td></tr></table>";
    }

    private async Task<Dictionary<(TicketType, int), string?>> ResolveDatesAsync(IReadOnlyList<OrderMailTicket> tickets)
    {
        var dates = new Dictionary<(TicketType, int), string?>();
        foreach (var ticket in tickets)
        {
            var key = (ticket.Type, ticket.ScopeId);
            if (dates.ContainsKey(key)) continue;
            dates[key] = ticket.Type == TicketType.EventTicket
                ? await events.FindByIdAsync(ticket.ScopeId) is { } ev
                    ? ev.TimeUnknown ? $"{ev.Date:dd.MM.yyyy}" : $"{ev.Date:dd.MM.yyyy}, {ev.StartTime:HH:mm} Uhr"
                    : null
                : await seasons.FindByIdAsync(ticket.ScopeId) is { } season
                    ? $"{season.StartDate:dd.MM.yyyy} – {season.EndDate:dd.MM.yyyy}"
                    : null;
        }
        return dates;
    }

    private string TicketCard(string baseUrl, OrderMailTicket ticket, string? dateText, int index, int total)
    {
        var token = tokens.Create(ticket.Type, ticket.Uuid, ticket.ScopeId);
        var url = $"{baseUrl}/ticket/{token}";
        var qrUrl = $"{url}/qr.png";
        var reference = ticket.Uuid.ToString("N")[..8].ToUpperInvariant();
        var scopeName = WebUtility.HtmlEncode(ticket.EventName);
        var category = WebUtility.HtmlEncode(ticket.CategoryName);
        var accent = TypeAccent(ticket.Type);
        var typeLabel = TypeLabel(ticket.Type);

        var rows =
            (dateText is null ? "" : MetaRow("Datum", WebUtility.HtmlEncode(dateText))) +
            MetaRow("Kategorie", category) +
            MetaRow("Ticket-Nr.", reference);

        var counter = total > 1
            ? $"<div style=\"max-width:400px;margin:0 auto 6px;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#888888;font-size:12px;text-transform:uppercase;letter-spacing:0.05em;\">Ticket {index} von {total}</div>"
            : "";

        return counter +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"max-width:400px;margin:0 auto 44px;background:#ffffff;border:1px solid #e5e7eb;border-radius:16px;overflow:hidden;page-break-inside:avoid;\">" +
                $"<tr><td style=\"background:{accent};padding:12px 16px;\">" +
                    "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>" +
                        "<td style=\"vertical-align:middle;\">" +
                            $"<div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;color:#ffffff;font-size:18px;font-weight:700;text-transform:uppercase;letter-spacing:0.04em;line-height:1.1;\">{typeLabel}</div>" +
                            $"<div style=\"font-family:Verdana,Geneva,Tahoma,sans-serif;color:#ffffff;opacity:.92;font-size:13px;padding-top:2px;\">{scopeName}</div>" +
                        "</td>" +
                        $"<td width=\"56\" align=\"right\" style=\"vertical-align:middle;\"><span style=\"display:inline-block;background:#ffffff;border-radius:8px;padding:4px 6px;line-height:0;\"><img src=\"{LogoUrl}\" alt=\"Red Ants Winterthur\" height=\"34\" style=\"height:34px;width:auto;display:block;\"></span></td>" +
                    "</tr></table>" +
                "</td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:16px 16px 4px;\"><img src=\"{qrUrl}\" alt=\"Ticket QR\" width=\"200\" height=\"200\" style=\"display:block;border:1px solid #eeeeee;border-radius:8px;\"></td></tr>" +
                "<tr><td align=\"center\" style=\"padding:2px 16px 10px;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#888888;font-size:12px;\">Am Eingang scannen lassen</td></tr>" +
                $"<tr><td style=\"padding:0 20px 6px;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">{rows}</table></td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:8px 16px 6px;\"><a href=\"{url}\" style=\"display:inline-block;background:{accent};color:#ffffff;text-decoration:none;font-family:'Oswald',Arial,Helvetica,sans-serif;font-weight:600;text-transform:uppercase;letter-spacing:0.04em;font-size:14px;padding:11px 22px;border-radius:6px;\">Online-Ticket öffnen</a></td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:0 16px 18px;font-family:Verdana,Geneva,Tahoma,sans-serif;font-size:12px;\"><a href=\"{url}/pdf\" style=\"color:#666666;text-decoration:underline;\">Als PDF</a>{(wallet.Enabled ? $" &nbsp;·&nbsp; <a href=\"{url}/wallet\" style=\"color:#666666;text-decoration:underline;\">In Google Wallet</a>" : "")}</td></tr>" +
            "</table>";
    }

    private static string MetaRow(string key, string value) =>
        "<tr>" +
            $"<td style=\"border-top:1px solid #f0f0f0;padding:6px 0;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#666666;font-size:13px;\">{key}</td>" +
            $"<td align=\"right\" style=\"border-top:1px solid #f0f0f0;padding:6px 0;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#101010;font-size:13px;font-weight:700;\">{value}</td>" +
        "</tr>";

    private static string TypeAccent(TicketType type) => type switch
    {
        TicketType.EventTicket => "#C8102E",
        TicketType.SeasonSingle => "#E4720F",
        TicketType.SeasonPass => "#1F5FBF",
        TicketType.MemberCard => "#1A7F37",
        TicketType.FreeEntry => "#6B4EA0",
        _ => "#C8102E"
    };

    private static string TypeLabel(TicketType type) => type switch
    {
        TicketType.EventTicket => "Spielticket",
        TicketType.SeasonSingle => "Flexticket",
        TicketType.SeasonPass => "Saisonkarte",
        TicketType.MemberCard => "Mitgliederkarte",
        TicketType.FreeEntry => "Freier Eintritt",
        _ => "Ticket"
    };
}
