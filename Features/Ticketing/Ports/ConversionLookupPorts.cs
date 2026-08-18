using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record ConversionOffer(
    TicketType CardType,
    Guid CardUuid,
    int SeasonId,
    int OriginCategory,
    int TierId,
    decimal Price,
    string CardLabel,
    int RemainingCap,
    string EventName,
    int? EventRemaining);

public sealed record ConversionResolution(bool Ok, string? Error, ConversionOffer? Offer);

public interface IConvertibleCards
{
    Task<ConversionResolution> ResolveAsync(int eventId, string cardNumber);

    Task MarkFlexConvertedAsync(Guid flexUuid, int eventId);
}
