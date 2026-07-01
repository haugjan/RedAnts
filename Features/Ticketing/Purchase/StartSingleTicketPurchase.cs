using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Purchase;

/// <summary>Creates a pending single ticket and initiates payment (Payrexx or local dev simulation).</summary>
public sealed class StartSingleTicketPurchase(IEvents events, ISingleTickets tickets, IPaymentGateway gateway)
{
    public async Task<PurchaseStarted> ExecuteAsync(StartSinglePurchaseCommand cmd, string baseUrl)
    {
        var evt = await events.FindByIdAsync(cmd.EventId)
                  ?? throw new DomainException("Anlass nicht gefunden.");
        if (evt.Status is EventStatus.Draft or EventStatus.Closed)
            throw new DomainException("Für diesen Anlass sind keine Tickets erhältlich.");

        var price = evt.PriceFor(cmd.Category)
                    ?? throw new DomainException("Für diese Kategorie ist kein Preis hinterlegt.");

        var billing = cmd.Billing.ToBillingAddress();
        var ticket = SingleTicket.CreatePending(evt.Id, cmd.Category, price, billing, PaymentMethod.Payrexx);
        var saved = await tickets.SaveAsync(ticket);

        var redirectUrl = await PaymentRedirect.BuildAsync(
            gateway, baseUrl, saved.TicketId, TicketKind.Single, saved.Price, billing.Email,
            purpose: $"Ticket {evt.Name}");

        return new PurchaseStarted(saved.TicketId, redirectUrl);
    }
}
