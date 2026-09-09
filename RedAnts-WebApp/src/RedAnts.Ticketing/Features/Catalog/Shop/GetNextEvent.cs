using RedAnts.Ticketing.Features.Checkout;

namespace RedAnts.Ticketing.Features.Catalog.Shop;

public sealed record NextEvent(
    int Id,
    string Name,
    DateOnly Date,
    TimeOnly StartTime,
    bool TimeUnknown,
    string? ImageUrl,
    string? HomeTeamLogoUrl,
    string? AwayTeamLogoUrl,
    string? VenueName,
    string? Url,
    string? AbsoluteUrl,
    IReadOnlyList<AvailableTicketCategory> Categories);

public static class GetNextEvent
{
    public sealed record Query;

    public sealed class Handler(INextEventReader reader)
    {
        public Task<NextEvent?> HandleAsync(Query query) => reader.FindAsync();
    }
}
