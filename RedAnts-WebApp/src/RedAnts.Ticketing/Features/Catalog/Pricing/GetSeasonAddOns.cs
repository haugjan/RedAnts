using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog.Pricing;

public sealed record SeasonAddOnRow(
    int Id,
    string Label,
    string? LongTitle,
    decimal Price,
    bool Active,
    AddOnScope Scope,
    string? InfoBeforePurchase,
    string? InfoAfterPurchase,
    IReadOnlyList<int> AllowedTierIds,
    bool PromoOnly,
    bool RequireMobileNumber);

public static class GetSeasonAddOns
{
    public sealed record Query(int SeasonId);

    public sealed class Handler(ISeasonsForAdminReader reader)
    {
        public Task<IReadOnlyList<SeasonAddOnRow>> HandleAsync(Query query) => reader.GetAddOnsAsync(query.SeasonId);
    }
}
