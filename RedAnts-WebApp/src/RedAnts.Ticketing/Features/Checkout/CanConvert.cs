using RedAnts.Ticketing.Features.MemberCards;

namespace RedAnts.Ticketing.Features.Checkout;

public static class CanConvert
{
    public sealed record Check(int EventId, string CardNumber, int? TierId = null);

    public sealed record CardNotUsable(string Reason) : CheckResult.Denied.Reason(Reason);

    public sealed record TierChoiceRequired(string Reason, IReadOnlyList<ConversionTierChoice> Choices)
        : CheckResult.Denied.Reason(Reason);

    public sealed class Handler(ICartRepository carts, IConvertibleCards convertibleCards)
    {
        public async Task<CheckResult> HandleAsync(Check check)
        {
            var resolution = await convertibleCards.ResolveAsync(check.EventId, check.CardNumber ?? "", check.TierId);
            if (resolution.Offer is not { } offer)
            {
                var reason = resolution.Error ?? "Umwandlung nicht möglich.";
                return resolution.TierChoices is { Count: > 0 } choices
                    ? CheckResult.Deny(new TierChoiceRequired(reason, choices))
                    : CheckResult.Deny(new CardNotUsable(reason));
            }

            return carts.Load().ConversionBlocker(check.EventId, offer.CardUuid, offer.RemainingCap, offer.EventRemaining);
        }
    }
}
