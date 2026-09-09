using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Features.Catalog;

namespace RedAnts.Ticketing.Features.Admission;

public sealed record ScannerEvent(
    int Id, string Name, DateOnly Date, TimeOnly StartTime, bool TimeUnknown, string VenueName, EventStatus Status);

public static class GetEventsForScanning
{
    public sealed record Query(IReadOnlyCollection<int>? OnlyEventIds = null);

    public sealed class Handler(IEventReader events, IVenueReader venues)
    {
        public async Task<IReadOnlyList<ScannerEvent>> HandleAsync(Query query)
        {
            var venueNames = (await venues.GetAllAsync()).ToDictionary(v => v.Id, v => v.Name);
            return (await events.GetUpcomingForScanningAsync())
                .Where(e => query.OnlyEventIds is null || query.OnlyEventIds.Contains(e.Id))
                .Select(e => new ScannerEvent(e.Id, e.Name, e.Date, e.StartTime, e.TimeUnknown,
                    venueNames.GetValueOrDefault(e.VenueId) ?? "", e.Status))
                .ToList();
        }
    }
}
