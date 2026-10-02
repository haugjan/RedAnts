using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;

namespace RedAnts.Ticketing.Features.Checkout;

public static class GetQuickBuyCart
{
    public const int MaxPerCategory = 9;

    public sealed record Line(int TierId, int Quantity);

    public sealed record Query(int EventId, IReadOnlyList<Line> Lines);

    public sealed class Handler(IEventPricing pricing, IEventReader events, IEventConversionRuleReader conversionRules)
    {
        public async Task<Cart?> HandleAsync(Query query)
        {
            if (await conversionRules.GetConversionOnlyAsync(query.EventId)) return null;
            var evt = await events.FindByIdAsync(query.EventId);
            if (evt is null) return null;

            var wanted = query.Lines
                .Where(l => l.Quantity > 0)
                .GroupBy(l => l.TierId)
                .Select(g => new Line(g.Key, g.Sum(l => l.Quantity)))
                .ToList();

            var cart = Cart.Empty();
            foreach (var line in wanted)
            {
                var available = await pricing.FindAvailableByTierAsync(query.EventId, line.TierId);
                if (available is not { Available: true }) continue;

                var quantity = Math.Min(line.Quantity, MaxPerCategory);
                if (available.Remaining is { } remaining) quantity = Math.Min(quantity, remaining);
                if (quantity < 1) continue;

                cart.AddEventTickets(query.EventId, evt.Name, available.TierId, available.Name, available.StandardName(),
                    Money.Of(available.Price), quantity);
            }

            return cart.IsEmpty ? null : cart;
        }
    }
}
