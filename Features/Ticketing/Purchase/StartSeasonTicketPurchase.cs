using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Purchase;

/// <summary>Creates a pending season ticket and initiates payment (Payrexx or local dev simulation).</summary>
public sealed class StartSeasonTicketPurchase(
    ISeasons seasons, ISeasonTickets tickets, ISeasonTicketPricing pricing, IPaymentGateway gateway)
{
    public async Task<PurchaseStarted> ExecuteAsync(StartSeasonPurchaseCommand cmd, string baseUrl)
    {
        var season = await seasons.FindByIdAsync(cmd.SeasonId)
                     ?? throw new DomainException("Saison nicht gefunden.");
        if (season.Status is SeasonStatus.Draft or SeasonStatus.Closed)
            throw new DomainException("Für diese Saison sind keine Saisonkarten erhältlich.");

        var price = pricing.PriceFor(cmd.Category, cmd.AgeGroup);

        var billing = cmd.Billing.ToBillingAddress();
        var ticket = SeasonTicket.CreatePending(season.Id, cmd.Category, cmd.AgeGroup, price, billing, PaymentMethod.Payrexx);
        var saved = await tickets.SaveAsync(ticket);

        var redirectUrl = await PaymentRedirect.BuildAsync(
            gateway, baseUrl, saved.SeasonTicketId, TicketKind.Season, saved.Price, billing.Email,
            purpose: $"Saisonkarte {season.Name}");

        return new PurchaseStarted(saved.SeasonTicketId, redirectUrl);
    }
}
