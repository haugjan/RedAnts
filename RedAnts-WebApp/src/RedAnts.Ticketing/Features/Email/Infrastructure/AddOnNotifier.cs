using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Features.Shared;

namespace RedAnts.Ticketing.Features.Email.Infrastructure;

public sealed class AddOnNotifier(IEmailSender email, IConfiguration config) : IAddOnNotifier
{
    public async Task NotifyAsync(string orderNumber, string buyerName, string buyerEmail,
        IReadOnlyList<OrderAddOnLine> lines, CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0) return;

        var recipient = config["Ticketing:AdminEmail"]
            ?? config["Graph:Sender"]
            ?? "tickets@redants.ch";

        var rows = string.Join("\n", lines.Select(l =>
            $"{l.Quantity}× {l.Label} ({l.SeasonName}{(string.IsNullOrWhiteSpace(l.CategoryName) ? "" : ", " + l.CategoryName)}) – CHF {RedAnts.Ticketing.Features.Shared.MoneyFormat.Chf(l.Price * l.Quantity)}"));
        var total = lines.Sum(l => l.Price * l.Quantity);

        var body = $"Zu Bestellung {orderNumber} wurden Zusatzoptionen zu Saisonkarten gewählt:\n\n{rows}";
        var details = $"Käufer: {buyerName}\nE-Mail: {buyerEmail}\nSumme Zusatzoptionen: CHF {RedAnts.Ticketing.Features.Shared.MoneyFormat.Chf(total)}";

        var html = EmailLayout.Render(
            "Neue Zusatzoption bestellt",
            body,
            details: details,
            note: "Diese Meldung dient der manuellen Weiterverarbeitung, zum Beispiel dem Freischalten eines Livestream-Zugangs.");

        await email.SendAsync(recipient, "Red Ants Ticketing",
            $"Zusatzoption bestellt – {orderNumber}", html, null, cancellationToken,
            source: "Zusatzoption", reference: orderNumber);
    }
}
