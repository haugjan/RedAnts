using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CheckoutWorkflow;

public static class GetQuickBuyCart
{
    public sealed record Query(int EventId, int TierId);

    public sealed class Handler(IEventPricing pricing, IEvents events, IEventConversionRules conversionRules)
    {
        public async Task<Cart?> HandleAsync(Query query)
        {
            if (await conversionRules.GetConversionOnlyAsync(query.EventId)) return null;
            var available = await pricing.FindAvailableByTierAsync(query.EventId, query.TierId);
            var evt = await events.FindByIdAsync(query.EventId);
            if (available is not { Available: true } || evt is null) return null;

            var cart = Cart.Empty();
            cart.AddEventTickets(query.EventId, evt.Name, available.TierId, available.Name, available.StandardName(), available.Price, 1);
            return cart;
        }
    }
}
