using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Email;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Tickets;
using RedAnts.Infrastructure.Shared;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class FlexTicketMailer(
    IEmailSender email,
    ITicketTokens tokens,
    IQrCodeRenderer qr,
    ISeasons seasons,
    IPublicBaseUrl publicUrl,
    ITicketingMailSettings settings,
    IWebHostEnvironment environment,
    ILogger<FlexTicketMailer> logger) : IFlexTicketMailer
{
    private const string BadgeLogoFile = "logo-badge-mail.png";

    private const string FallbackSubject = "Dein Red Ants Flexticket";

    private const string FallbackBody =
        "Hallo {Name}\n\n" +
        "Hier ist dein Flexticket der Red Ants Rychenberg Winterthur für die {Saison}. Zeige den QR-Code am Eingang, auf dem Handy oder ausgedruckt.\n\n" +
        "Damit hast du an einem Heimspiel deiner Wahl freien Eintritt.\n\n" +
        "Wir freuen uns auf dich in der Halle!\n\n" +
        "Sportliche Grüsse";

    public string DefaultSubject => settings.Subject(TicketingMailKind.FlexTicket, FallbackSubject);

    public string DefaultBody => settings.Body(TicketingMailKind.FlexTicket, FallbackBody);

    public async Task<EmailSendResult> SendAsync(FlexMailTicket ticket, string subject, string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticket.Email))
            return new EmailSendResult(false, "Für dieses Flexticket ist keine E-Mail hinterlegt.");

        try
        {
            var season = await seasons.FindByIdAsync(ticket.SeasonId);
            var resolvedSubject = Fill(subject, ticket, season?.Name);
            var intro = MailMarkdown.ToHtml(Fill(body, ticket, season?.Name));

            var images = new List<EmailAttachment>();
            var html = EmailLayout.Render(resolvedSubject, intro + BuildCard(ticket, season, images), greeting: null,
                note: "Fragen? Antworte einfach auf diese E-Mail.");

            var reference = ticket.Uuid.ToString("N")[..8].ToUpperInvariant();
            var toName = string.IsNullOrWhiteSpace(ticket.HolderName) ? null : ticket.HolderName;

            var result = await email.SendAsync(ticket.Email, toName, resolvedSubject, html, images,
                cancellationToken, source: "Flexticket", reference: reference);
            if (!result.Success)
                logger.LogWarning("Flex ticket e-mail to {Recipient} failed: {Error}", ticket.Email, result.Error);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Flex ticket e-mail to {Recipient} threw.", ticket.Email);
            return new EmailSendResult(false, ex.Message);
        }
    }

    private string BuildCard(FlexMailTicket ticket, Season? season, List<EmailAttachment> images)
    {
        const string red = "#D02D38";
        var url = publicUrl.TicketUrl(tokens.CreateShort(ticket.Uuid));
        var badgeCid = AddImageFile(images, BadgeLogoFile);
        var qrCid = AddQrImage(images, url);
        var reference = ticket.Uuid.ToString("N")[..8].ToUpperInvariant();
        var holder = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(ticket.HolderName) ? "Flexticket" : ticket.HolderName);
        var category = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(ticket.CategoryLabel) ? "Flexticket" : ticket.CategoryLabel);
        var typeLabel = TicketDisplay.TypeLabel(TicketType.SeasonSingle);
        var kicker = TicketDisplay.Kicker(TicketType.SeasonSingle);

        var rows =
            (season is null ? "" : InfoRow("Saison", WebUtility.HtmlEncode(season.Name))) +
            InfoRow("Kategorie", category) +
            InfoRow("Ticket-Nr.", reference);

        return
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" class=\"ra-ticket\" style=\"margin:24px auto 14px;max-width:420px;background:#ffffff;border:1px solid #e6e8ec;border-radius:16px;overflow:hidden;page-break-inside:avoid;\">" +
                $"<tr><td style=\"background:{red};padding:14px 18px 15px;\">" +
                    "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>" +
                        "<td style=\"vertical-align:top;\">" +
                            $"<div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;color:#ffffff;font-size:12px;font-weight:500;text-transform:uppercase;letter-spacing:1.4px;opacity:.92;\">{kicker}</div>" +
                            $"<div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;color:#ffffff;font-size:26px;font-weight:700;text-transform:uppercase;line-height:1;padding-top:2px;\">{typeLabel}</div>" +
                        "</td>" +
                        $"<td align=\"right\" style=\"vertical-align:top;\"><img src=\"{badgeCid}\" alt=\"Red Ants\" width=\"52\" height=\"52\" style=\"display:block;width:52px;height:52px;border-radius:50%;\"></td>" +
                    "</tr></table>" +
                "</td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:18px 10px 4px;\"><img src=\"{qrCid}\" alt=\"Ticket QR\" width=\"300\" height=\"300\" class=\"ra-qr\" style=\"display:block;margin:0 auto;width:300px;max-width:100%;height:auto;\"></td></tr>" +
                "<tr><td align=\"center\" style=\"padding:0 16px 12px;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#6b7178;font-size:13px;\">Am Eingang scannen lassen</td></tr>" +
                "<tr><td style=\"padding:0 12px;\"><div style=\"border-top:2px dashed #d6dade;font-size:0;line-height:0;\">&nbsp;</div></td></tr>" +
                $"<tr><td style=\"padding:14px 20px 2px;\"><div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;color:#14171A;font-size:18px;font-weight:600;text-transform:uppercase;line-height:1.1;\">{holder}</div></td></tr>" +
                $"<tr><td style=\"padding:0 20px 14px;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">{rows}</table></td></tr>" +
            "</table>" +
            MailTicketActions.Render(url, "Online-Ticket öffnen", $"{TicketDisplay.TypeLabel(TicketType.SeasonSingle)} – {season?.Name ?? "Flexticket"}");
    }

    private static string InfoRow(string key, string value) =>
        "<tr>" +
            $"<td style=\"border-top:1px solid #eef0f2;padding:6px 0;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#6b7178;font-size:14px;\">{key}</td>" +
            $"<td align=\"right\" style=\"border-top:1px solid #eef0f2;padding:6px 0;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#14171A;font-size:14px;font-weight:700;\">{value}</td>" +
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

    private string AddQrImage(List<EmailAttachment> images, string url)
    {
        const string fileName = "qr-card.png";
        var cid = ContentIdFor(fileName);
        images.Add(new EmailAttachment(fileName, Convert.ToBase64String(qr.RenderPng(url, 10)), "image/png", cid));
        return $"cid:{cid}";
    }

    private static string Fill(string text, FlexMailTicket ticket, string? seasonName) =>
        (text ?? "")
            .Replace("{Name}", ticket.HolderName ?? "")
            .Replace("{Saison}", seasonName ?? "");
}
