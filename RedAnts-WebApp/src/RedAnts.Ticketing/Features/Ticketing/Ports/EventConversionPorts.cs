using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record EventConversionRule(int EventId, TicketType CardType, decimal Discount);

public interface IEventConversionRules
{
    Task<IReadOnlyList<EventConversionRule>> GetAllAsync();

    Task<IReadOnlyList<EventConversionRule>> GetByEventAsync(int eventId);

    Task SetAsync(int eventId, TicketType cardType, decimal? discount);

    Task<bool> GetConversionOnlyAsync(int eventId);

    Task SetConversionOnlyAsync(int eventId, bool value);
}
