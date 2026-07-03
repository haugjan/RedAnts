using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

/// <summary>A purchasable category for an event with its sale price and remaining availability.</summary>
public sealed record AvailableTicketCategory(
    TicketCategory Category,
    string Name,
    decimal Price,
    bool Available,
    int? Remaining);

/// <summary>Read/write of an event's price set (0..1 per event) with its per-category prices/quotas
/// and the event-level sales/admission quotas. Managed in the ticketing admin.</summary>
public interface IEventPrices
{
    Task<EventPrice?> GetByEventAsync(int eventId);
    Task<EventPrice> SaveAsync(EventPrice price);
    Task DeleteAsync(int eventPriceId);
}

/// <summary>Read/write of a season's price set (0..1 per season) with its per-category prices/quotas.</summary>
public interface ISeasonPrices
{
    Task<SeasonPrice?> GetBySeasonAsync(int seasonId);
    Task<SeasonPrice> SaveAsync(SeasonPrice price);
    Task DeleteAsync(int seasonPriceId);
}

/// <summary>Read side: resolves the purchasable categories for an event, applying the sale price and
/// the availability rules (a category is sold out when its own quota or the event's total sales quota
/// is reached).</summary>
public interface IEventPricing
{
    Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int eventId);
    Task<AvailableTicketCategory?> FindAvailableAsync(int eventId, TicketCategory category);
}

/// <summary>Issued event tickets (single-event admission).</summary>
public interface IEventTickets
{
    Task<IReadOnlyList<EventTicket>> GetByEventAsync(int eventId);
    Task<EventTicket> SaveAsync(EventTicket ticket);
}
