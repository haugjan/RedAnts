using System.Text;
using System.Text.Json;
using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Shared;

namespace RedAnts.Infrastructure.Ticketing;

/// <summary>Sends ticket confirmation e-mails via the Brevo transactional REST API.</summary>
public sealed class BrevoTicketEmail(IHttpClientFactory httpClientFactory, IConfiguration config) : ITicketEmail
{
    private const string SendUrl = "https://api.brevo.com/v3/smtp/email";

    public Task SendSingleTicketConfirmationAsync(SingleTicket ticket, Event evt)
    {
        var details =
            $"Anlass: {evt.Name}\n" +
            $"Datum: {evt.Date:dd.MM.yyyy} {evt.StartTime:HH\\:mm}\n" +
            $"Kategorie: {Labels.PriceCategory(ticket.PriceCategory)}\n" +
            $"Preis: CHF {ticket.Price:N2}\n" +
            $"Ticket-ID: {ticket.TicketId}";

        var html = EmailLayout.Render(
            title: "Ticket bestätigt",
            body: "Vielen Dank für deinen Kauf. Dein Ticket ist bestätigt und bezahlt.",
            greeting: $"Guten Tag {ticket.BillingAddress.FirstName} {ticket.BillingAddress.LastName}",
            details: details,
            note: "Bitte bewahre diese Bestätigung auf. Der QR-Code für den Einlass folgt separat.");

        return SendAsync(ticket.BillingAddress.Email, "Ticket bestätigt", html);
    }

    public Task SendSeasonTicketConfirmationAsync(SeasonTicket ticket, Season season)
    {
        var details =
            $"Saison: {season.Name}\n" +
            $"Kategorie: {Labels.SeasonTicketCategory(ticket.Category)}\n" +
            $"Altersgruppe: {Labels.AgeGroup(ticket.AgeGroup)}\n" +
            $"Preis: CHF {ticket.Price:N2}\n" +
            $"Saisonkarten-ID: {ticket.SeasonTicketId}";

        var html = EmailLayout.Render(
            title: "Saisonkarte bestätigt",
            body: "Vielen Dank für deinen Kauf. Deine Saisonkarte ist bestätigt und bezahlt.",
            greeting: $"Guten Tag {ticket.BillingAddress.FirstName} {ticket.BillingAddress.LastName}",
            details: details,
            note: "Bitte bewahre diese Bestätigung auf. Der QR-Code für den Einlass folgt separat.");

        return SendAsync(ticket.BillingAddress.Email, "Saisonkarte bestätigt", html);
    }

    private async Task SendAsync(string toEmail, string subject, string html)
    {
        var apiKey = config["Brevo:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return; // not configured (dev) -> no-op

        var senderName = config["Brevo:SenderName"] ?? "Red Ants Ticketing";
        var senderEmail = config["Brevo:SenderEmail"] ?? "noreply@redants.localhost";
        var bcc = config["Brevo:AdminBcc"];

        var payload = new Dictionary<string, object?>
        {
            ["sender"] = new { name = senderName, email = senderEmail },
            ["to"] = new[] { new { email = toEmail } },
            ["subject"] = subject,
            ["htmlContent"] = html
        };
        if (!string.IsNullOrWhiteSpace(bcc))
            payload["bcc"] = new[] { new { email = bcc } };

        var client = httpClientFactory.CreateClient("Brevo");
        using var request = new HttpRequestMessage(HttpMethod.Post, SendUrl);
        request.Headers.Add("api-key", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        await client.SendAsync(request);
    }
}
