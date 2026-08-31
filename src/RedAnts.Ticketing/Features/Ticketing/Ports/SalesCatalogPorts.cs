using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record AvailableTicketCategory(
    int TierId,
    string Name,
    decimal Price,
    bool Available,
    int? Remaining,
    DateOnly? AvailableUntil = null,
    string? ShortName = null,
    string? ActionText = null,
    decimal? OriginalPrice = null,
    int? MinAge = null,
    int? MaxAge = null)
{
    public string? AgeText => (MinAge, MaxAge) switch
    {
        ({ } lo, { } hi) => $"{lo} bis {hi} Jahre",
        ({ } lo, null) => $"ab {lo} Jahren",
        (null, { } hi) => $"bis {hi} Jahre",
        _ => null
    };
}

public interface IPriceTiers
{
    Task<IReadOnlyList<PriceTier>> GetBySeasonAsync(int seasonId);
    Task<IReadOnlyList<PriceTier>> SaveForSeasonAsync(int seasonId, IReadOnlyList<PriceTierInput> tiers);
    Task<int> GetSoldCountAsync(int tierId);
}

public sealed record PriceTierInput(int Id, string Name, int? MinAge, int? MaxAge, int SortOrder, PriceTierPromoInput? Promo);

public sealed record PriceTierPromoInput(int Id, string Name);

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

public sealed record TicketDemand(int EventId, int TierId, int Quantity, bool IsConversion = false);

public interface IEventPricing
{
    Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int eventId);
    Task<AvailableTicketCategory?> FindAvailableByTierAsync(int eventId, int tierId);
    Task<string?> CheckCapacityAsync(IReadOnlyList<TicketDemand> demand);
}

public sealed record PassDemand(int SeasonId, int TierId, int Quantity);

public interface ISeasonPassPricing
{
    Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int seasonId);
    Task<AvailableTicketCategory?> FindAvailableByTierAsync(int seasonId, int tierId);
    Task<string?> CheckCapacityAsync(IReadOnlyList<PassDemand> demand);
    Task<IReadOnlyDictionary<int, int>> GetSoldCountsAsync(int seasonId);
}

public interface IEventTickets
{
    Task<IReadOnlyList<EventTicket>> GetByEventAsync(int eventId);
    Task<IReadOnlyList<EventTicket>> GetByOrderAsync(int orderId);
    Task<EventTicket> SaveAsync(EventTicket ticket);
    Task SetHolderAsync(Guid uuid, CardHolder holder);
}

public interface ISeasonPasses
{
    Task<SeasonPass?> GetByUuidAsync(Guid uuid);
    Task<IReadOnlyList<SeasonPass>> GetByOrderAsync(int orderId);
    Task<SeasonPass> SaveAsync(SeasonPass pass);
    Task SetHolderAsync(Guid uuid, CardHolder holder);
    Task<(int Created, int Updated)> ImportUnifiedAsync(int seasonId, IReadOnlyList<TicketImportRow> rows,
        string defaultBundle, int? defaultTierId = null, string? createdByName = null, string? createdByEmail = null);
}

public interface IOrders
{
    Task<Order> SaveAsync(Order order);
    Task<string> NextOrderNumberAsync();
    Task<Order?> GetByIdAsync(int id);
    Task<Order?> GetByNumberAsync(string orderNumber);
    Task<bool> TryMarkPaidAsync(int orderId);
    Task CopyBillingToTicketsAsync(int orderId);
}

public interface ISeasonAddOns
{
    Task<IReadOnlyList<SeasonAddOn>> GetBySeasonAsync(int seasonId);
    Task ReplaceForSeasonAsync(int seasonId, IReadOnlyList<SeasonAddOn> options);
}

public sealed record OrderAddOnLine(
    int SeasonId, string SeasonName, TicketCategory Category, string CategoryName,
    string Label, decimal Price, int Quantity, int? TierId = null);

public interface IOrderAddOns
{
    Task SaveAsync(int orderId, IReadOnlyList<OrderAddOnLine> lines);
    Task<IReadOnlyList<OrderAddOnLine>> GetByOrderAsync(int orderId);
}
