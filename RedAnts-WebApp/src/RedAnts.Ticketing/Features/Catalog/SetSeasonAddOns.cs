using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog;

public sealed record SeasonAddOnInput(
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

public static class SetSeasonAddOns
{
    public sealed record Command(int SeasonId, IReadOnlyList<SeasonAddOnInput> AddOns);

    public sealed class Handler(ISeasonAddOns addOns, IPriceTiers tiers)
    {
        public async Task HandleAsync(Command command)
        {
            var validTierIds = (await tiers.GetBySeasonAsync(command.SeasonId))
                .Where(t => t.PromoOfTierId is null)
                .Select(t => t.Id)
                .ToHashSet();

            var options = command.AddOns
                .Where(a => !string.IsNullOrWhiteSpace(a.Label))
                .Select(a => SeasonAddOn.Create(command.SeasonId, a.Label, decimal.Round(a.Price, 2), a.Active, 0, a.Scope,
                    a.InfoBeforePurchase, a.InfoAfterPurchase, a.LongTitle,
                    a.AllowedTierIds.Where(validTierIds.Contains).ToList(), a.PromoOnly, a.RequireMobileNumber))
                .ToList();

            await addOns.ReplaceForSeasonAsync(command.SeasonId, options);
        }
    }
}
