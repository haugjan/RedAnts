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

public sealed record PassDemand(int SeasonId, TicketCategory Category, int Quantity);

public interface ISeasonPassPricing
{
    Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int seasonId);
    Task<string?> CheckCapacityAsync(IReadOnlyList<PassDemand> demand);
    Task<IReadOnlyDictionary<TicketCategory, int>> GetSoldCountsAsync(int seasonId);
}

public interface IEventTickets
{
    Task<IReadOnlyList<EventTicket>> GetByEventAsync(int eventId);
    Task<EventTicket> SaveAsync(EventTicket ticket);
}

public sealed record SeasonPassImportAddress(
    string? Street, string? PostalCode, string? City, string? Country, string? Email, string? Phone)
{
    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Street) && !string.IsNullOrWhiteSpace(City) && !string.IsNullOrWhiteSpace(Email);
}

public sealed record SeasonPassImportRow(string Reference, TicketCategory Category, Buyer Buyer, SeasonPassImportAddress Address);

public interface ISeasonPasses
{
    Task<SeasonPass?> GetByUuidAsync(Guid uuid);
    Task<SeasonPass> SaveAsync(SeasonPass pass);
    Task<int> ImportAsync(int seasonId, IReadOnlyList<SeasonPassImportRow> rows,
        string? createdByName = null, string? createdByEmail = null);
}

public interface IOrders
{
    Task<Order> SaveAsync(Order order);
    Task<string> NextOrderNumberAsync();
    Task<Order?> GetByIdAsync(int id);
}
