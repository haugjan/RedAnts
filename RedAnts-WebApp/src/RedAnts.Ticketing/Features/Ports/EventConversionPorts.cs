using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Ports;

public sealed record EventConversionRule(int EventId, TicketType CardType, decimal Discount);

public interface IEventConversionRules
{
    Task<IReadOnlyList<EventConversionRule>> GetAllAsync();

    Task<IReadOnlyList<EventConversionRule>> GetByEventAsync(int eventId);

    Task SetAsync(int eventId, TicketType cardType, decimal? discount);

    Task<bool> GetConversionOnlyAsync(int eventId);

    Task SetConversionOnlyAsync(int eventId, bool value);
}
