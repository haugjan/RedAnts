using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.AdmissionWorkflow;

public static class GetOccupancy
{
    public sealed record Query(int EventId);

    public sealed class Handler(IOccupancyReader occupancy)
    {
        public Task<Occupancy> HandleAsync(Query query) => occupancy.GetAsync(query.EventId);
    }
}
