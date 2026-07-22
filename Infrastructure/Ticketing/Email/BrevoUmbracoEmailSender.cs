using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models.Email;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class BrevoUmbracoEmailSender(
    IServiceScopeFactory scopeFactory,
    IConfiguration config) : Umbraco.Cms.Core.Mail.IEmailSender
{
    public bool CanSendRequiredEmail() =>
        !string.IsNullOrWhiteSpace(config["Brevo:ApiKey"])
        && !string.IsNullOrWhiteSpace(config["Brevo:SenderEmail"]);

    public Task SendAsync(EmailMessage message, string emailType) =>
        SendAsync(message, emailType, false);

    public async Task SendAsync(EmailMessage message, string emailType, bool enableNotification)
    {
        var recipients = (message.To ?? []).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
        if (recipients.Length == 0) return;

        var body = message.IsBodyHtml
            ? message.Body ?? ""
            : $"<pre style=\"font-family:inherit;white-space:pre-wrap\">{WebUtility.HtmlEncode(message.Body ?? "")}</pre>";

        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<RedAnts.Features.Ticketing.Email.IEmailSender>();

        foreach (var to in recipients)
        {
            if (string.IsNullOrWhiteSpace(to)) continue;
            var (address, name) = ParseRecipient(to);
            var result = await sender.SendAsync(address, name, message.Subject ?? "", body);
            if (!result.Success)
                throw new InvalidOperationException($"E-Mail an {address} konnte nicht gesendet werden: {result.Error}");
        }
    }

    private static (string Address, string? Name) ParseRecipient(string recipient)
    {
        try
        {
            var parsed = new System.Net.Mail.MailAddress(recipient);
            return (parsed.Address, string.IsNullOrWhiteSpace(parsed.DisplayName) ? null : parsed.DisplayName);
        }
        catch (FormatException)
        {
            return (recipient.Trim(), null);
        }
    }
}
