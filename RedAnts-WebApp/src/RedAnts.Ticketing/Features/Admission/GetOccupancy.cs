using RedAnts.Ticketing.Domain.Admission;

namespace RedAnts.Ticketing.Features.Admission;

public static class GetOccupancy
{
    public sealed record Query(int EventId);

    public sealed class Handler(IOccupancyReader occupancy)
    {
        public Task<Occupancy> HandleAsync(Query query) => occupancy.GetAsync(query.EventId);
    }
}
