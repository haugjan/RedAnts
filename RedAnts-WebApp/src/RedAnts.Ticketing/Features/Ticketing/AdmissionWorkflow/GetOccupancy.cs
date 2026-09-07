using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.AdmissionWorkflow;

public static class GetOccupancy
{
    public sealed record Query(int EventId);

    public sealed class Handler(IOccupancyReader occupancy)
    {
        public Task<Occupancy> HandleAsync(Query query) => occupancy.GetAsync(query.EventId);
    }
}
