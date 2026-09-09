namespace RedAnts.Ticketing.Features.Catalog.Shop;

public sealed record TicketingHome(IReadOnlyList<UpcomingEvent> Events, IReadOnlyList<SeasonPassOffers> PassOffers);

public static class GetTicketingHome
{
    public sealed record Query;

    public sealed class Handler(ITicketingHomeReader reader)
    {
        public Task<TicketingHome> HandleAsync(Query query) => reader.GetAsync();
    }
}
