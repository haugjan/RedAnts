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

public sealed class SeasonPassMailer(
    IEmailSender email,
    ITicketTokens tokens,
    IQrCodeRenderer qr,
    ISeasons seasons,
    IPublicBaseUrl publicUrl,
    IWebHostEnvironment environment,
    ILogger<SeasonPassMailer> logger) : ISeasonPassMailer
{
    private const string BadgeLogoFile = "logo-badge-mail.png";

    public string DefaultSubject => "Deine Red Ants Saisonkarte";

    public string DefaultBody =>
        "Hallo {Name}\n\n" +
        "Hier ist deine persönliche Saisonkarte der Red Ants Rychenberg Winterthur für die {Saison}. Zeige den QR-Code am Eingang, auf dem Handy oder ausgedruckt.\n\n" +
        "Damit hast du an allen Heimspielen der Saison freien Eintritt.\n\n" +
        "Vielen Dank für deine Unterstützung. Bis bald in der Halle!\n\n" +
        "Sportliche Grüsse";

    public async Task<EmailSendResult> SendAsync(SeasonPass pass, string categoryLabel, string subject, string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pass.Email))
            return new EmailSendResult(false, "Für diese Saisonkarte ist keine E-Mail hinterlegt.");

        try
        {
            var season = await seasons.FindByIdAsync(pass.SeasonId);
            var resolvedSubject = Fill(subject, pass, season?.Name);
            var intro = MailMarkdown.ToHtml(Fill(body, pass, season?.Name));

            var images = new List<EmailAttachment>();
            var html = EmailLayout.Render(resolvedSubject, intro + BuildCard(pass, season, categoryLabel, images), greeting: null,
                note: "Fragen? Antworte einfach auf diese E-Mail.");

            var reference = string.IsNullOrWhiteSpace(pass.Reference)
                ? pass.Uuid.ToString("N")[..8].ToUpperInvariant()
                : pass.Reference;
            var toName = pass.Buyer?.DisplayName is { Length: > 0 } n ? n : null;

            var result = await email.SendAsync(pass.Email, toName, resolvedSubject, html, images,
                cancellationToken, source: "Saisonkarte", reference: reference);
            if (!result.Success)
                logger.LogWarning("Season pass e-mail to {Recipient} failed: {Error}", pass.Email, result.Error);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Season pass e-mail to {Recipient} threw.", pass.Email);
            return new EmailSendResult(false, ex.Message);
        }
    }

    private string BuildCard(SeasonPass pass, Season? season, string categoryLabel, List<EmailAttachment> images)
    {
        const string red = "#D02D38";
        var token = tokens.Create(TicketType.SeasonPass, pass.Uuid, pass.SeasonId);
        var url = $"{publicUrl.Resolve()}/ticket/{token}";
        var badgeCid = AddImageFile(images, BadgeLogoFile);
        var qrCid = AddQrImage(images, url);
        var reference = pass.Uuid.ToString("N")[..8].ToUpperInvariant();
        var holderName = pass.Buyer?.DisplayName is { Length: > 0 } n ? n : "Saisonkarte";
        var holder = WebUtility.HtmlEncode(holderName);
        var category = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(categoryLabel) ? pass.Category.DisplayName() : categoryLabel);
        var typeLabel = TicketDisplay.TypeLabel(TicketType.SeasonPass);
        var kicker = TicketDisplay.Kicker(TicketType.SeasonPass);

        var rows =
            (season is null ? "" : InfoRow("Saison", WebUtility.HtmlEncode(season.Name))) +
            InfoRow("Kategorie", category) +
            InfoRow("Karten-Nr.", reference);

        return
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" class=\"ra-ticket\" style=\"margin:24px auto 20px;max-width:420px;background:#ffffff;border:1px solid #e6e8ec;border-radius:16px;overflow:hidden;page-break-inside:avoid;\">" +
                $"<tr><td style=\"background:{red};padding:14px 18px 15px;\">" +
                    "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>" +
                        "<td style=\"vertical-align:top;\">" +
                            $"<div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;color:#ffffff;font-size:12px;font-weight:500;text-transform:uppercase;letter-spacing:1.4px;opacity:.92;\">{kicker}</div>" +
                            $"<div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;color:#ffffff;font-size:26px;font-weight:700;text-transform:uppercase;line-height:1;padding-top:2px;\">{typeLabel}</div>" +
                        "</td>" +
                        $"<td align=\"right\" style=\"vertical-align:top;\"><img src=\"{badgeCid}\" alt=\"Red Ants\" width=\"52\" height=\"52\" style=\"display:block;width:52px;height:52px;border-radius:50%;\"></td>" +
                    "</tr></table>" +
                "</td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:18px 10px 4px;\"><img src=\"{qrCid}\" alt=\"Karten QR\" width=\"300\" height=\"300\" class=\"ra-qr\" style=\"display:block;margin:0 auto;width:300px;max-width:100%;height:auto;\"></td></tr>" +
                "<tr><td align=\"center\" style=\"padding:0 16px 12px;font-family:Verdana,Geneva,Tahoma,sans-serif;color:#6b7178;font-size:13px;\">Am Eingang scannen lassen</td></tr>" +
                "<tr><td style=\"padding:0 12px;\"><div style=\"border-top:2px dashed #d6dade;font-size:0;line-height:0;\">&nbsp;</div></td></tr>" +
                $"<tr><td style=\"padding:14px 20px 2px;\"><div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;color:#14171A;font-size:18px;font-weight:600;text-transform:uppercase;line-height:1.1;\">{holder}</div></td></tr>" +
                $"<tr><td style=\"padding:0 20px 8px;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">{rows}</table></td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:6px 16px 6px;\"><a href=\"{url}\" style=\"display:inline-block;background:{red};color:#ffffff;text-decoration:none;font-family:'Oswald',Arial,Helvetica,sans-serif;font-weight:600;text-transform:uppercase;letter-spacing:0.7px;font-size:14px;padding:11px 22px;border-radius:8px;\">Online-Karte öffnen</a></td></tr>" +
                $"<tr><td align=\"center\" style=\"padding:0 16px 18px;font-family:Verdana,Geneva,Tahoma,sans-serif;font-size:12px;\"><a href=\"{url}/pdf\" style=\"color:#666666;text-decoration:underline;\">Als PDF</a></td></tr>" +
            "</table>";
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

    private static string Fill(string text, SeasonPass pass, string? seasonName) =>
        (text ?? "")
            .Replace("{Vorname}", pass.Buyer?.FirstName ?? "")
            .Replace("{Nachname}", pass.Buyer?.LastName ?? "")
            .Replace("{Name}", pass.Buyer?.DisplayName ?? "")
            .Replace("{Saison}", seasonName ?? "");
}
