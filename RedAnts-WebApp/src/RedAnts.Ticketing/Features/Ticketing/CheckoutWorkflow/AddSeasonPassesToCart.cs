using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CheckoutWorkflow;

public static class AddSeasonPassesToCart
{
    public sealed record Command(int SeasonId, int TierId, int Quantity, IReadOnlyList<int> AddOnIds);

    public sealed record Result(bool Added, string CategoryName, Cart Cart);

    public sealed class Handler(ICartRepository carts, ISeasonPassPricing passPricing, ISeasons seasons, ISeasonAddOns seasonAddOns, IPriceTiers priceTiers)
    {
        public async Task<Result> HandleAsync(Command command)
        {
            var quantity = Math.Max(1, command.Quantity);
            var available = await passPricing.FindAvailableByTierAsync(command.SeasonId, command.TierId);
            var season = await seasons.FindByIdAsync(command.SeasonId);
            if (available is not { Available: true } || season is null)
                return new Result(false, available?.Name ?? "", carts.Load());

            var chosen = await ChosenAddOnsAsync(command, available);
            var perPass = chosen.Where(a => a.Scope == AddOnScope.PerPass).Select(a => ToCartAddOn(a, season)).ToList();
            var perOrder = chosen.Where(a => a.Scope == AddOnScope.PerOrder).Select(a => ToCartAddOn(a, season)).ToList();

            var cart = carts.Load();
            cart.AddSeasonPasses(command.SeasonId, season.Name, available.TierId, available.Name, available.StandardName(), available.Price, quantity, perPass);
            cart.AddOrderAddOns(perOrder);
            carts.Save(cart);
            return new Result(true, available.Name, cart);
        }

        private async Task<List<SeasonAddOn>> ChosenAddOnsAsync(Command command, AvailableTicketCategory available)
        {
            var selectedIds = (command.AddOnIds ?? []).ToHashSet();
            if (selectedIds.Count == 0) return [];

            var baseByTier = (await priceTiers.GetBySeasonAsync(command.SeasonId)).ToDictionary(t => t.Id, t => t.PromoOfTierId ?? t.Id);
            var baseTierId = baseByTier.GetValueOrDefault(available.TierId, available.TierId);
            var isPromoOffer = baseTierId != available.TierId;
            return (await seasonAddOns.GetBySeasonAsync(command.SeasonId))
                .Where(a => a.Active && selectedIds.Contains(a.Id)
                    && (a.AllowedTierIds.Count == 0 || a.AllowedTierIds.Contains(baseTierId))
                    && (!a.PromoOnly || isPromoOffer))
                .ToList();
        }

        private static CartAddOn ToCartAddOn(SeasonAddOn addOn, Season season) =>
            new(addOn.Id, addOn.Label, addOn.Price, season.Id, season.Name, addOn.RequireMobileNumber);
    }
}
