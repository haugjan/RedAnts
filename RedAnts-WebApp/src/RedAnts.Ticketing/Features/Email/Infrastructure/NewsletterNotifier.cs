using RedAnts.Ticketing.Features.Shared;

namespace RedAnts.Ticketing.Features.Email.Infrastructure;

public sealed class NewsletterNotifier(IEmailSender sender, IConfiguration config) : INewsletterNotifier
{
    public async Task NotifyNewSignupAsync(string email, string? name, string source, CancellationToken cancellationToken = default)
    {
        var recipient = config["Ticketing:AdminEmail"]
            ?? config["Graph:Sender"]
            ?? "tickets@redants.ch";

        var displayName = string.IsNullOrWhiteSpace(name) ? "ohne Namensangabe" : name!.Trim();
        var details = $"E-Mail: {email}\nName: {displayName}\nQuelle: {source}\nAngemeldet am: {SwissTime.Now:dd.MM.yyyy HH:mm}";

        var html = EmailLayout.Render(
            "Neue Newsletter-Anmeldung",
            "Eine Adresse hat sich für den Newsletter angemeldet und war noch nicht auf der Liste.",
            details: details,
            note: "Die Adresse steht im Backoffice unter Newsletter als offene Anmeldung bereit und lässt sich dort als Fairgate-CSV exportieren.");

        await sender.SendAsync(recipient, "Red Ants Ticketing",
            "Neue Newsletter-Anmeldung", html, null, cancellationToken,
            source: "Newsletter", reference: email);
    }
}
