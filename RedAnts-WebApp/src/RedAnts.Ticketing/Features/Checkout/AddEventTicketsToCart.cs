using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;

namespace RedAnts.Ticketing.Features.Checkout;

public static class AddEventTicketsToCart
{
    public sealed record Command(int EventId, int TierId, int Quantity);

    public sealed record Result(bool Added, string CategoryName, string? Message, Cart Cart);

    public sealed class Handler(ICartRepository carts, IEventConversionRuleRepository conversionRules, IEventPricing pricing, IEventReader events)
    {
        public async Task<Result> HandleAsync(Command command)
        {
            var quantity = Math.Max(1, command.Quantity);
            if (await conversionRules.GetConversionOnlyAsync(command.EventId))
                return new Result(false, "", "Für diesen Anlass sind normale Ticketkäufe nicht möglich (nur Kartenumwandlung).", carts.Load());

            var available = await pricing.FindAvailableByTierAsync(command.EventId, command.TierId);
            var evt = await events.FindByIdAsync(command.EventId);
            if (available is not { Available: true } || evt is null)
                return new Result(false, available?.Name ?? "", null, carts.Load());

            var cart = carts.Load();
            cart.AddEventTickets(command.EventId, evt.Name, available.TierId, available.Name, available.StandardName(), available.Price, quantity);
            carts.Save(cart);
            return new Result(true, available.Name, null, cart);
        }
    }
}
