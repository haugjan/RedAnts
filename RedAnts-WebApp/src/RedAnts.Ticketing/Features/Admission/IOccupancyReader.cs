using RedAnts.Ticketing.Domain.Admission;

namespace RedAnts.Ticketing.Features.Admission;

public interface IOccupancyReader
{
    Task<Occupancy> GetAsync(int eventId);
}
