using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CheckoutWorkflow;

public static class AddConversionToCart
{
    public sealed record Command(int EventId, string CardNumber, int? TierId);

    public sealed record Result(bool Added, string Message, bool NeedsTier, IReadOnlyList<ConversionTierChoice> TierChoices, Cart Cart);

    public sealed class Handler(ICartRepository carts, IConvertibleCards convertibleCards)
    {
        public async Task<Result> HandleAsync(Command command)
        {
            var resolution = await convertibleCards.ResolveAsync(command.EventId, command.CardNumber ?? "", command.TierId);
            var cart = carts.Load();
            if (!resolution.Ok || resolution.Offer is null)
            {
                var choices = resolution.TierChoices ?? [];
                return new Result(false, resolution.Error ?? "Umwandlung nicht möglich.", choices.Count > 0, choices, cart);
            }

            var offer = resolution.Offer;
            var inCartForEvent = cart.Items
                .Where(i => i.Kind == CartLineKind.EventTicket && i.EventId == command.EventId)
                .Sum(i => i.Quantity);
            if (offer.EventRemaining is { } remaining && inCartForEvent >= remaining)
                return new Result(false, "Für diesen Anlass sind keine Tickets mehr verfügbar (Kontingent ausgeschöpft).", false, [], cart);

            var origin = new ConversionOrigin(offer.CardType, offer.CardUuid, offer.CardLabel, offer.OriginCategory, offer.RemainingCap);
            var added = cart.AddConversion(command.EventId, offer.EventName, offer.SeasonId, offer.TierId, offer.CardLabel, offer.Price, origin);
            if (added == 0)
                return new Result(false, "Für diese Karte sind bereits alle Umwandlungen im Warenkorb.", false, [], cart);

            carts.Save(cart);
            var message = offer.Price > 0m
                ? $"Umgewandeltes Ticket ({offer.CardLabel}) für CHF {offer.Price:0.00} im Warenkorb."
                : $"Umgewandeltes Ticket ({offer.CardLabel}) im Warenkorb.";
            return new Result(true, message, false, [], cart);
        }
    }
}
