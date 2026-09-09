using RedAnts.Ticketing.Features.Checkout;

namespace RedAnts.Ticketing.Features.SeasonPasses;

public sealed record PassDemand(int SeasonId, int TierId, int Quantity);

public interface ISeasonPassPricing
{
    Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int seasonId);
    Task<AvailableTicketCategory?> FindAvailableByTierAsync(int seasonId, int tierId);
    Task<string?> CheckCapacityAsync(IReadOnlyList<PassDemand> demand);
    Task<IReadOnlyDictionary<int, int>> GetSoldCountsAsync(int seasonId);
}
