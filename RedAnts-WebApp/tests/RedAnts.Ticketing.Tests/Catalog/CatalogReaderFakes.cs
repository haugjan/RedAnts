using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Catalog.Pricing;
using RedAnts.Ticketing.Features.Catalog.Shop;

namespace RedAnts.Ticketing.Tests.Catalog;

internal sealed class StubSeasonsForAdmin : ISeasonsForAdminReader
{
    public SeasonsForAdmin Seasons { get; set; } = new([], null);
    public List<SeasonChoice> Choices { get; } = [];
    public Dictionary<int, IReadOnlyList<PriceTierRow>> Tiers { get; } = new();
    public Dictionary<int, IReadOnlyList<SeasonTierPrice>> TierPrices { get; } = new();
    public Dictionary<int, IReadOnlyList<SeasonAddOnRow>> AddOns { get; } = new();

    public Task<SeasonsForAdmin> GetAllAsync() => Task.FromResult(Seasons);

    public Task<IReadOnlyList<SeasonChoice>> GetChoicesAsync() => Task.FromResult<IReadOnlyList<SeasonChoice>>(Choices);

    public Task<IReadOnlyList<PriceTierRow>> GetTiersAsync(int seasonId) => Task.FromResult(Tiers.GetValueOrDefault(seasonId) ?? []);

    public Task<IReadOnlyList<SeasonTierPrice>> GetTierPricesAsync(int seasonId) => Task.FromResult(TierPrices.GetValueOrDefault(seasonId) ?? []);

    public Task<IReadOnlyList<SeasonAddOnRow>> GetAddOnsAsync(int seasonId) => Task.FromResult(AddOns.GetValueOrDefault(seasonId) ?? []);
}

internal sealed class StubEventsForAdmin : IEventsForAdminReader
{
    public Dictionary<int, EventsForAdmin> BySeason { get; } = new();
    public Dictionary<int, IReadOnlyList<EventTierPrice>> TierPrices { get; } = new();

    public Task<EventsForAdmin> GetBySeasonAsync(int seasonId) =>
        Task.FromResult(BySeason.GetValueOrDefault(seasonId) ?? new EventsForAdmin([], null));

    public Task<IReadOnlyList<EventTierPrice>> GetTierPricesAsync(int eventId) => Task.FromResult(TierPrices.GetValueOrDefault(eventId) ?? []);
}

internal sealed class StubTicketingHome : ITicketingHomeReader
{
    public TicketingHome Home { get; set; } = new([], []);

    public Task<TicketingHome> GetAsync() => Task.FromResult(Home);
}

internal sealed class StubEventForSale : IEventForSaleReader
{
    public Dictionary<int, EventForSale> Events { get; } = new();

    public Task<EventForSale?> FindAsync(int eventId) => Task.FromResult(Events.GetValueOrDefault(eventId));
}

internal sealed class StubSeasonForSale : ISeasonForSaleReader
{
    public Dictionary<int, SeasonForSale> Seasons { get; } = new();
    public List<SeasonPassOffers> PassOffers { get; } = [];

    public Task<SeasonForSale?> FindAsync(int seasonId) => Task.FromResult(Seasons.GetValueOrDefault(seasonId));

    public Task<IReadOnlyList<SeasonPassOffers>> GetPassOffersAsync() => Task.FromResult<IReadOnlyList<SeasonPassOffers>>(PassOffers);
}

internal sealed class StubNextEvent : INextEventReader
{
    public NextEvent? Next { get; set; }

    public Task<NextEvent?> FindAsync() => Task.FromResult(Next);
}

internal sealed class StubVenues : IVenueReader
{
    public List<Venue> Venues { get; } = [];

    public Task<IReadOnlyList<Venue>> GetAllAsync() => Task.FromResult<IReadOnlyList<Venue>>(Venues);

    public Task<Venue?> FindByIdAsync(int id) => Task.FromResult(Venues.FirstOrDefault(v => v.Id == id));
}
