using RedAnts.Ticketing.Features.Catalog.Pricing;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IEventsForAdminReader
{
    Task<EventsForAdmin> GetBySeasonAsync(int seasonId);

    Task<IReadOnlyList<EventTierPrice>> GetTierPricesAsync(int eventId);
}
