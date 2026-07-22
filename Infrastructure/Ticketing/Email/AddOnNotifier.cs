using Microsoft.Extensions.Configuration;
using RedAnts.Features.Ticketing.Email;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Shared;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class AddOnNotifier(IEmailSender email, IConfiguration config) : IAddOnNotifier
{
    public async Task NotifyAsync(string orderNumber, string buyerName, string buyerEmail,
        IReadOnlyList<OrderAddOnLine> lines, CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0) return;

        var recipient = config["Ticketing:AdminEmail"]
            ?? config["Brevo:SenderEmail"]
            ?? "tickets@redants.ch";

        var rows = string.Join("\n", lines.Select(l =>
            $"{l.Quantity}× {l.Label} ({l.SeasonName}{(string.IsNullOrWhiteSpace(l.CategoryName) ? "" : ", " + l.CategoryName)}) – CHF {RedAnts.Features.Ticketing.MoneyFormat.Chf(l.Price * l.Quantity)}"));
        var total = lines.Sum(l => l.Price * l.Quantity);

        var body = $"Zu Bestellung {orderNumber} wurden Zusatzoptionen zu Saisonkarten gewählt:\n\n{rows}";
        var details = $"Käufer: {buyerName}\nE-Mail: {buyerEmail}\nSumme Zusatzoptionen: CHF {RedAnts.Features.Ticketing.MoneyFormat.Chf(total)}";

        var html = EmailLayout.Render(
            "Neue Zusatzoption bestellt",
            body,
            details: details,
            note: "Diese Meldung dient der manuellen Weiterverarbeitung, zum Beispiel dem Freischalten eines Livestream-Zugangs.");

        await email.SendAsync(recipient, "Red Ants Ticketing",
            $"Zusatzoption bestellt – {orderNumber}", html, cancellationToken);
    }
}
