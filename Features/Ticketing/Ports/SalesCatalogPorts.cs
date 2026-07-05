using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record AvailableTicketCategory(
    TicketCategory Category,
    string Name,
    decimal Price,
    bool Available,
    int? Remaining);

public interface IEventPrices
{
    Task<EventPrice?> GetByEventAsync(int eventId);
    Task<EventPrice> SaveAsync(EventPrice price);
    Task DeleteAsync(int eventPriceId);
}

public interface ISeasonPrices
{
    Task<SeasonPrice?> GetBySeasonAsync(int seasonId);
    Task<SeasonPrice> SaveAsync(SeasonPrice price);
    Task DeleteAsync(int seasonPriceId);
}

public sealed record TicketDemand(int EventId, TicketCategory Category, int Quantity);

public interface IEventPricing
{
    Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int eventId);
    Task<AvailableTicketCategory?> FindAvailableAsync(int eventId, TicketCategory category);
    Task<string?> CheckCapacityAsync(IReadOnlyList<TicketDemand> demand);
}

public interface IEventTickets
{
    Task<IReadOnlyList<EventTicket>> GetByEventAsync(int eventId);
    Task<EventTicket> SaveAsync(EventTicket ticket);
}

public interface ISeasonPasses
{
    Task<SeasonPass?> GetByUuidAsync(Guid uuid);
    Task<SeasonPass> SaveAsync(SeasonPass pass);
}

public interface IOrders
{
    Task<Order> SaveAsync(Order order);
    Task<string> NextOrderNumberAsync();
}
