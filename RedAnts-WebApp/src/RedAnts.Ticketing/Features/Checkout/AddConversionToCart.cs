using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.MemberCards;

namespace RedAnts.Ticketing.Features.Checkout;

public static class AddConversionToCart
{
    public sealed record Command(int EventId, string CardNumber, int? TierId);

    public sealed record Result(bool Added, string Message, Cart Cart);

    public sealed class Handler(ICartRepository carts, IConvertibleCards convertibleCards)
    {
        public async Task<Result> HandleAsync(Command command)
        {
            var resolution = await convertibleCards.ResolveAsync(command.EventId, command.CardNumber ?? "", command.TierId);
            var cart = carts.Load();
            if (resolution.Offer is not { } offer)
                return new Result(false, resolution.Error ?? "Umwandlung nicht möglich.", cart);

            if (cart.ConversionBlocker(command.EventId, offer.CardUuid, offer.RemainingCap, offer.EventRemaining) is CheckResult.Denied blocked)
                return new Result(false, blocked.Cause.Message, cart);

            var origin = new ConversionOrigin(offer.CardType, offer.CardUuid, offer.CardLabel, offer.OriginCategory, offer.RemainingCap);
            if (cart.AddConversion(command.EventId, offer.EventName, offer.SeasonId, offer.TierId, offer.CardLabel, offer.Price, origin) == 0)
                return new Result(false, new ConversionDenied.CardExhausted().Message, cart);

            carts.Save(cart);
            var message = offer.Price > 0m
                ? $"Umgewandeltes Ticket ({offer.CardLabel}) für CHF {offer.Price:0.00} im Warenkorb."
                : $"Umgewandeltes Ticket ({offer.CardLabel}) im Warenkorb.";
            return new Result(true, message, cart);
        }
    }
}
