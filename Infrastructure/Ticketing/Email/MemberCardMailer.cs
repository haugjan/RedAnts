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

public sealed class MemberCardMailer(
    IEmailSender email,
    ITicketTokens tokens,
    IQrCodeRenderer qr,
    ISeasons seasons,
    IPublicBaseUrl publicUrl,
    ITicketingMailSettings settings,
    IWebHostEnvironment environment,
    ILogger<MemberCardMailer> logger) : IMemberCardMailer
{
    private const string BadgeLogoFile = "logo-badge-mail.png";

    private const string FallbackSubject = "Deine Red Ants Mitgliederkarte";

    private const string FallbackBody =
        "Hallo {Vorname}\n\n" +
        "Hier ist deine persönliche Mitgliederkarte der Red Ants Rychenberg Winterthur für die {Saison}. Zeige den QR-Code am Eingang, auf dem Handy oder ausgedruckt.\n\n" +
        "Mit deiner Mitgliederkarte profitierst du ausserdem von diesen Vorteilen:\n\n" +
        "- Ochsner Sport Archhöfe, Winterthur: 20% auf Hallenschuhe und 30% auf Fat Pipe und Blindsave Unihockey-Artikel unseres Goldsponsors Fat Pipe (Sportagon), oder mit dem Rabattcode 6y76z2fq bei sau.ch.\n" +
        "- Eisen Optikergeschäft AG: eine Schutzbrille pro Saison für CHF 10.-, gegen Vorweisen des Mitgliederausweises oder Angabe des Namens. Der Sehtest ist für alle im gleichen Haushalt lebenden Personen gratis.\n" +
        "- Restaurant La Pergola: jeweils am Samstagmittag ein Pastamenü mit Salat und 5dl-Getränk für CHF 20.-, gegen Vorweisen des Mitgliederausweises.\n\n" +
        "Vielen Dank für deine Unterstützung. Bis bald in der Halle!\n\n" +
        "Sportliche Grüsse";

    public string DefaultSubject => settings.Subject(TicketingMailKind.MemberCard, FallbackSubject);

    public string DefaultBody => settings.Body(TicketingMailKind.MemberCard, FallbackBody);

    public async Task<EmailSendResult> SendAsync(MemberCard card, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(card.Email))
            return new EmailSendResult(false, "Für diese Karte ist keine E-Mail hinterlegt.");

        try
        {
            var season = await seasons.FindByIdAsync(card.SeasonId);
            var resolvedSubject = Fill(subject, card, season?.Name);
            var intro = MailMarkdown.ToHtml(Fill(body, card, season?.Name));

            var images = new List<EmailAttachment>();
            var html = EmailLayout.Render(resolvedSubject, intro + BuildCard(card, season, images), greeting: null,
                note: "Fragen? Antworte einfach auf diese E-Mail.");

            var reference = string.IsNullOrWhiteSpace(card.Reference)
                ? card.Uuid.ToString("N")[..8].ToUpperInvariant()
                : card.Reference;
            var toName = string.IsNullOrWhiteSpace(card.HolderName) ? null : card.HolderName;

            var result = await email.SendAsync(card.Email, toName, resolvedSubject, html, images,
                cancellationToken, source: "Mitgliederkarte", reference: reference);
            if (!result.Success)
                logger.LogWarning("Member card e-mail to {Recipient} failed: {Error}", card.Email, result.Error);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Member card e-mail to {Recipient} threw.", card.Email);
            return new EmailSendResult(false, ex.Message);
        }
    }

    private string BuildCard(MemberCard card, Season? season, List<EmailAttachment> images)
    {
        const string red = "#D02D38";
        var url = publicUrl.TicketUrl(tokens.CreateShort(card.Uuid));
        var badgeCid = AddImageFile(images, BadgeLogoFile);
        var qrCid = AddQrImage(images, url);
        var reference = card.Uuid.ToString("N")[..8].ToUpperInvariant();
        var scope = WebUtility.HtmlEncode(season?.Name ?? "Mitgliederkarte");
        var typeLabel = WebUtility.HtmlEncode(card.Category.DisplayName());
        var kicker = TicketDisplay.Kicker(TicketType.MemberCard);
        var dateText = season is null ? null : $"{season.StartDate:dd.MM.yyyy} – {season.EndDate:dd.MM.yyyy}";

        var rows =
            (dateText is null ? "" : InfoRow("Datum", WebUtility.HtmlEncode(dateText))) +
            (string.IsNullOrWhiteSpace(card.HolderName) ? "" : InfoRow("Inhaber:in", WebUtility.HtmlEncode(card.HolderName))) +
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
                $"<tr><td style=\"padding:14px 20px 2px;\"><div style=\"font-family:'Oswald',Arial,Helvetica,sans-serif;color:#14171A;font-size:18px;font-weight:600;text-transform:uppercase;line-height:1.1;\">{scope}</div></td></tr>" +
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

    private static string Fill(string text, MemberCard card, string? seasonName) =>
        (text ?? "")
            .Replace("{Vorname}", card.FirstName ?? "")
            .Replace("{Nachname}", card.LastName ?? "")
            .Replace("{Name}", card.HolderName)
            .Replace("{Saison}", seasonName ?? "");
}
