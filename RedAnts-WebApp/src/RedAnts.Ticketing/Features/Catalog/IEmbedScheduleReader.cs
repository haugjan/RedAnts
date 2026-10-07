using RedAnts.Ticketing.Features.Catalog.Shop;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IEmbedScheduleReader
{
    Task<IReadOnlyList<EmbedGame>> GetUpcomingAsync();
}
