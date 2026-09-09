using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog;

public sealed record EventConversionRule(int EventId, TicketType CardType, decimal Discount);

public interface IEventConversionRuleRepository
{
    Task SetAsync(int eventId, TicketType cardType, decimal? discount);

    Task<bool> GetConversionOnlyAsync(int eventId);

    Task SetConversionOnlyAsync(int eventId, bool value);
}
