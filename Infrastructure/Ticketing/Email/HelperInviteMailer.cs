using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Email;
using RedAnts.Infrastructure.Shared;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class HelperInviteMailer(
    IEmailSender email,
    IConfiguration config,
    IWebHostEnvironment hostEnvironment,
    ILogger<HelperInviteMailer> logger) : IHelperInviteMailer
{
    public string DefaultSubject => "Dein Zugang zum Red Ants Scan-Tool";

    public string DefaultBody =>
        "Hallo {Vorname} {Nachname}\n\n" +
        "vielen Dank, dass du beim Einlass der Red Ants mithilfst! Mit deinem persönlichen Zugang " +
        "kannst du am Anlass die Tickets scannen.\n\n" +
        "Deinen Login-Link und dein Passwort findest du unten. Eine kurze Anleitung zum Scan-Tool " +
        "liegt dieser E-Mail als PDF bei.\n\n" +
        "Bei Fragen antworte einfach auf diese E-Mail. Bis bald in der Halle!";

    public async Task<EmailSendResult> SendAsync(
        Helper helper, string subject, string body, string loginLink, CancellationToken cancellationToken = default)
    {
        var resolvedSubject = Fill(subject, helper, loginLink);
        var greeting = $"Hallo {helper.FirstName}".Trim() + ",";
        var bodyHtml = WebUtility.HtmlEncode(Fill(body, helper, loginLink)).Replace("\r\n", "\n");

        var access =
            $"<strong>Dein Zugang</strong><br>" +
            $"Passwort: <code>{WebUtility.HtmlEncode(helper.Code)}</code><br>" +
            $"Login-Link: <a href=\"{WebUtility.HtmlEncode(loginLink)}\">{WebUtility.HtmlEncode(loginLink)}</a>";

        var note = "Die Anleitung zum Scan-Tool findest du im PDF-Anhang dieser E-Mail.";
        var html = EmailLayout.Render(resolvedSubject, bodyHtml, greeting, access, note);

        var attachments = LoadGuideAttachment();
        return await email.SendAsync(helper.Email, helper.FullName, resolvedSubject, html, attachments, cancellationToken);
    }

    private static string Fill(string text, Helper helper, string loginLink) =>
        (text ?? "")
            .Replace("{Vorname}", helper.FirstName)
            .Replace("{Nachname}", helper.LastName)
            .Replace("{Passwort}", helper.Code)
            .Replace("{Link}", loginLink);

    private IReadOnlyList<EmailAttachment>? LoadGuideAttachment()
    {
        var configured = config["Scanner:GuidePdfPath"];
        var path = !string.IsNullOrWhiteSpace(configured)
            ? (Path.IsPathRooted(configured) ? configured : Path.Combine(hostEnvironment.ContentRootPath, configured))
            : Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "downloads", "scanner-anleitung.pdf");

        if (!File.Exists(path))
        {
            logger.LogWarning("Helper invite: guide PDF not found at {Path}; sending without attachment.", path);
            return null;
        }

        var bytes = File.ReadAllBytes(path);
        return [new EmailAttachment("Scanner-Anleitung.pdf", Convert.ToBase64String(bytes))];
    }
}
