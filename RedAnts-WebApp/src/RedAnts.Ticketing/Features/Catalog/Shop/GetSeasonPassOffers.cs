using RedAnts.Ticketing.Features.Checkout;

namespace RedAnts.Ticketing.Features.Catalog.Shop;

public sealed record PassAddOn(int Id, string Label, string? LongTitle, decimal Price, bool OncePerOrder, string? InfoBeforePurchase);

public sealed record PassOffer(AvailableTicketCategory Category, IReadOnlyList<PassAddOn> AddOns);

public sealed record SeasonPassOffers(int SeasonId, string SeasonName, string? Url, IReadOnlyList<PassOffer> Offers);

public static class GetSeasonPassOffers
{
    public sealed record Query;

    public sealed class Handler(ISeasonForSaleReader reader)
    {
        public Task<IReadOnlyList<SeasonPassOffers>> HandleAsync(Query query) => reader.GetPassOffersAsync();
    }
}
