using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog.Pricing;

public sealed record SeasonPassPricingInput(bool Offered, decimal Price, int? Quota, DateOnly? AvailableFrom, DateOnly? AvailableUntil);

public sealed record SeasonTicketPricingInput(bool Offered, decimal Price, int? Quota, DateOnly? AvailableUntil);

public sealed record SeasonTierPricingInput(
    int TierId,
    string Name,
    int? MinAge,
    int? MaxAge,
    SeasonPassPricingInput Pass,
    SeasonTicketPricingInput Ticket,
    int PromoTierId,
    string? PromoName,
    SeasonPassPricingInput? PromoPass,
    SeasonTicketPricingInput? PromoTicket)
{
    public bool HasPromo => !string.IsNullOrWhiteSpace(PromoName);
}

public static class SetSeasonPricing
{
    public sealed record Command(int SeasonId, IReadOnlyList<SeasonTierPricingInput> Tiers);

    public sealed class Handler(IPriceTierRepository tiers, ISeasonPriceRepository prices, ITierSalesReader sales)
    {
        private static readonly SeasonPassPricingInput NoPass = new(false, 0m, null, null, null);
        private static readonly SeasonTicketPricingInput NoTicket = new(false, 0m, null, null);

        public async Task HandleAsync(Command command)
        {
            var ordered = command.Tiers.Where(t => !string.IsNullOrWhiteSpace(t.Name)).ToList();
            await RejectRemovingSoldTiersAsync(command.SeasonId, ordered);

            var tierInputs = ordered
                .Select((t, sortOrder) => new PriceTierInput(t.TierId, t.Name.Trim(), t.MinAge, t.MaxAge, sortOrder,
                    t.HasPromo ? new PriceTierPromoInput(t.PromoTierId, t.PromoName!.Trim()) : null))
                .ToList();
            var saved = await tiers.SaveForSeasonAsync(command.SeasonId, tierInputs);

            var savedNormals = saved.Where(t => t.PromoOfTierId is null).OrderBy(t => t.SortOrder).ThenBy(t => t.Id).ToList();
            var savedPromoByParent = saved.Where(t => t.PromoOfTierId is not null).ToDictionary(t => t.PromoOfTierId!.Value);

            var categories = new List<SeasonCategoryPrice>();
            for (var i = 0; i < ordered.Count && i < savedNormals.Count; i++)
            {
                var input = ordered[i];
                var normal = savedNormals[i];
                categories.Add(Category(input.Pass, input.Ticket, normal.Id));
                if (savedPromoByParent.GetValueOrDefault(normal.Id) is { } promo)
                    categories.Add(Category(input.PromoPass ?? NoPass, input.PromoTicket ?? NoTicket, promo.Id));
            }

            var existing = await prices.GetBySeasonAsync(command.SeasonId);
            var price = existing?.WithCategories(categories) ?? SeasonPrice.Create(command.SeasonId, null, categories);
            await prices.SaveAsync(price);
        }

        private async Task RejectRemovingSoldTiersAsync(int seasonId, IReadOnlyList<SeasonTierPricingInput> ordered)
        {
            var kept = new HashSet<int>();
            foreach (var t in ordered)
            {
                if (t.TierId > 0) kept.Add(t.TierId);
                if (t.HasPromo && t.PromoTierId > 0) kept.Add(t.PromoTierId);
            }

            foreach (var tier in await tiers.GetBySeasonAsync(seasonId))
            {
                if (kept.Contains(tier.Id)) continue;
                if (await sales.GetSoldCountAsync(tier.Id) > 0)
                    throw new DomainException($"Die Stufe «{tier.Name}» kann nicht gelöscht werden, weil bereits Karten verkauft wurden.");
            }
        }

        private static SeasonCategoryPrice Category(SeasonPassPricingInput pass, SeasonTicketPricingInput ticket, int tierId) =>
            SeasonCategoryPrice.Create(default,
                decimal.Round(pass.Price, 2), pass.Offered, pass.Quota,
                decimal.Round(ticket.Price, 2), ticket.Offered, ticket.Quota,
                pass.AvailableFrom, pass.AvailableUntil, ticket.AvailableUntil, tierId);
    }
}
