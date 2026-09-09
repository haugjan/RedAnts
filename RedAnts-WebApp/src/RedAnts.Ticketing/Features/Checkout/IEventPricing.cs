namespace RedAnts.Ticketing.Features.Checkout;

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

public sealed record TicketDemand(int EventId, int TierId, int Quantity, bool IsConversion = false);

public interface IEventPricing
{
    Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int eventId);
    Task<AvailableTicketCategory?> FindAvailableByTierAsync(int eventId, int tierId);
    Task<string?> CheckCapacityAsync(IReadOnlyList<TicketDemand> demand);
}
