using System.Net;
using Microsoft.AspNetCore.Hosting;
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
    IQrCodeRenderer qr,
    IEvents events,
    ISeasons seasons,
    IVenues venues,
    IWebHostEnvironment environment,
    ILogger<OrderMailer> logger) : IOrderMailer
{
    private const string HeaderLogoFile = "mail-header-logo.png";
    private const string BadgeLogoFile = "logo-badge-mail.png";

    public async Task<bool> SendTicketsAsync(OrderMailModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var (subject, html, images) = await BuildAsync(model);
            var result = await email.SendAsync(model.ToEmail, model.ToName, subject, html, images, cancellationToken);
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

    public async Task<string> RenderAsync(OrderMailModel model)
    {
        var (_, html, images) = await BuildAsync(model);
        return InlinePreviewImages(html, images);
    }

    private async Task<(string Subject, string Html, List<EmailAttachment> Images)> BuildAsync(OrderMailModel model)
    {
        var images = new List<EmailAttachment>();
        var subject = $"Deine Tickets – Bestellung {model.OrderNumber}";
        var greeting = string.IsNullOrWhiteSpace(model.ToName) ? "Hallo," : $"Hallo {model.ToName},";
        var body = await BuildBodyAsync(model, images);
        var details = $"Bestellung {model.OrderNumber}\nTotal: CHF {RedAnts.Features.Ticketing.MoneyFormat.Chf(model.Total)}";
        var note = "Zeige den QR-Code am Eingang, auf dem Handy oder ausgedruckt. Fragen? Antworte einfach auf diese E-Mail.";
        var headerLogo = AddImageFile(images, HeaderLogoFile);
        var html = EmailLayout.Render(subject, body, greeting, details, note, headerLogo);
        return (subject, html, images);
    }

    private async Task<string> BuildBodyAsync(OrderMailModel model, List<EmailAttachment> images)
    {
        var intro = "<p style=\"margin:0 0 20px;\">Vielen Dank für deinen Kauf. Deine Tickets sind unten bereit, jedes mit eigenem QR-Code für den Einlass.</p>";
        var dates = await ResolveDatesAsync(model.Tickets);
        var venueNames = await ResolveVenuesAsync(model.Tickets);
        var total = model.Tickets.Count;
        var blocks = string.Concat(model.Tickets.Select((t, i) =>
            TicketCard(model.BaseUrl, t, dates.GetValueOrDefault((t.Type, t.ScopeId)), venueNames.GetValueOrDefault((t.Type, t.ScopeId)), i + 1, total, images)));
        return intro + blocks + AddOnInfoBlock(model.AddOnInfoTexts);
    }

    private static string AddOnInfoBlock(IReadOnlyList<string>? infos)
    {
        if (infos is null || infos.Count == 0) return "";
        var items = string.Concat(infos.Select(info =>
            $"<p style=\"margin:0 0 10px;\">{WebUtility.HtmlEncode(info).Replace("\n", "<br>")}</p>"));
        return "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:0 0 32px;background:#f5f5f5;border-left:4px solid #C8102E;border-radius:4px;\">" +
            "<tr><td style=\"padding:16px 18px;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#323232;font-size:16px;line-height:1.6;\">" +
                "<div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;font-weight:700;text-transform:uppercase;letter-spacing:0.04em;color:#101010;font-size:15px;margin:0 0 8px;\">Zu deinen Zusatzoptionen</div>" +
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

    private async Task<Dictionary<(TicketType, int), string?>> ResolveVenuesAsync(IReadOnlyList<OrderMailTicket> tickets)
    {
        var venueNames = new Dictionary<(TicketType, int), string?>();
        foreach (var ticket in tickets)
        {
            var key = (ticket.Type, ticket.ScopeId);
            if (venueNames.ContainsKey(key)) continue;
            if (ticket.Type == TicketType.EventTicket
                && await events.FindByIdAsync(ticket.ScopeId) is { VenueId: > 0 } ev)
                venueNames[key] = (await venues.FindByIdAsync(ev.VenueId))?.Name;
            else
                venueNames[key] = null;
        }
        return venueNames;
    }

    private string TicketCard(string baseUrl, OrderMailTicket ticket, string? dateText, string? venueText, int index, int total, List<EmailAttachment> images)
    {
        const string red = "#D02D38";
        const string redDk = "#B0242E";
        var token = tokens.Create(ticket.Type, ticket.Uuid, ticket.ScopeId);
        var url = $"{baseUrl}/ticket/{token}";
        var badgeCid = AddImageFile(images, BadgeLogoFile);
        var qrCid = AddQrImage(images, index, url);
        var reference = ticket.Uuid.ToString("N")[..8].ToUpperInvariant();
        var scopeName = WebUtility.HtmlEncode(ticket.EventName);
        var category = WebUtility.HtmlEncode(ticket.CategoryName);
        var typeLabel = TicketDisplay.TypeLabel(ticket.Type);
        var kicker = TicketDisplay.Kicker(ticket.Type);

        var rows =
            (dateText is null ? "" : InfoRow("Datum", WebUtility.HtmlEncode(dateText), redDk)) +
            (venueText is null ? "" : InfoRow("Ort", WebUtility.HtmlEncode(venueText), "#14171A")) +
            InfoRow("Kategorie", category, "#14171A") +
            InfoRow("Ticket-Nr.", reference, "#14171A");

        var counter = total > 1
            ? $"<div style=\"margin:0 0 6px;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#888888;font-size:13px;text-transform:uppercase;letter-spacing:0.05em;\">Ticket {index} von {total}</div>"
            : "";

        return counter +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:0 0 40px;background:#ffffff;border:1px solid #e6e8ec;border-radius:16px;overflow:hidden;page-break-inside:avoid;\">" +
                $"<tr><td style=\"background:{red};padding:14px 18px 15px;\">" +
                    "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>" +
                        "<td style=\"vertical-align:top;\">" +
                            $"<div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;color:#ffffff;font-size:13px;font-weight:500;text-transform:uppercase;letter-spacing:1.4px;opacity:.92;\">{kicker}</div>" +
                            $"<div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;color:#ffffff;font-size:28px;font-weight:700;text-transform:uppercase;line-height:1;padding-top:2px;\">{typeLabel}</div>" +
                        "</td>" +
                        $"<td align=\"right\" style=\"vertical-align:top;\"><img src=\"{badgeCid}\" alt=\"Red Ants\" width=\"52\" height=\"52\" style=\"display:block;width:52px;height:52px;border-radius:50%;\"></td>" +
                    "</tr></table>" +
                "</td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:18px 16px 4px;\"><img src=\"{qrCid}\" alt=\"Ticket QR\" width=\"300\" height=\"300\" style=\"display:block;margin:0 auto;max-width:100%;height:auto;\"></td></tr>" +
                "<tr><td align=\"center\" style=\"padding:0 16px 12px;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#6b7178;font-size:14px;\">Am Eingang scannen lassen</td></tr>" +
                "<tr><td style=\"padding:0 12px;\"><div style=\"border-top:2px dashed #d6dade;font-size:0;line-height:0;\">&nbsp;</div></td></tr>" +
                $"<tr><td style=\"padding:14px 20px 2px;\"><div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;color:#14171A;font-size:19px;font-weight:600;text-transform:uppercase;line-height:1.1;\">{scopeName}</div></td></tr>" +
                $"<tr><td style=\"padding:0 20px 8px;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">{rows}</table></td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:6px 16px 6px;\"><a href=\"{url}\" style=\"display:inline-block;background:{red};color:#ffffff;text-decoration:none;font-family:'Oswald',Arial,Helvetica,sans-serif;font-weight:600;text-transform:uppercase;letter-spacing:0.7px;font-size:15px;padding:11px 22px;border-radius:8px;\">Online-Ticket öffnen</a></td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:0 16px 18px;font-family:Verdana,Geneva,Tahoma,sans-serif;font-size:13px;\"><a href=\"{url}/pdf\" style=\"color:#666666;text-decoration:underline;\">Als PDF</a></td></tr>" +
            "</table>";
    }

    private static string InfoRow(string key, string value, string valueColor) =>
        "<tr>" +
            $"<td style=\"border-top:1px solid #eef0f2;padding:7px 0;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#6b7178;font-size:15px;\">{key}</td>" +
            $"<td align=\"right\" style=\"border-top:1px solid #eef0f2;padding:7px 0;font-family:Verdana,Geneva,Tahoma,sans-serif;color:{valueColor};font-size:15px;font-weight:700;\">{value}</td>" +
        "</tr>";

    private static string ContentIdFor(string fileName) => $"{Path.GetFileNameWithoutExtension(fileName)}@redants.ch";

    private string AddImageFile(List<EmailAttachment> images, string fileName)
    {
        var cid = ContentIdFor(fileName);
        if (images.Any(i => i.ContentId == cid)) return $"cid:{cid}";
        var path = Path.Combine(environment.WebRootPath, "img", fileName);
        if (!File.Exists(path)) return "";
        images.Add(new EmailAttachment(fileName, Convert.ToBase64String(File.ReadAllBytes(path)), "image/png", cid));
        return $"cid:{cid}";
    }

    private string AddQrImage(List<EmailAttachment> images, int index, string url)
    {
        var fileName = $"qr-{index}.png";
        var cid = ContentIdFor(fileName);
        images.Add(new EmailAttachment(fileName, Convert.ToBase64String(qr.RenderPng(url, 10)), "image/png", cid));
        return $"cid:{cid}";
    }

    private static string InlinePreviewImages(string html, IReadOnlyList<EmailAttachment> images)
    {
        foreach (var image in images)
            html = html.Replace($"cid:{image.ContentId}", $"data:{image.ContentType};base64,{image.Base64Content}");
        return html;
    }
}
