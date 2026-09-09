using RedAnts.Ticketing.Features.Stats.Admin;

namespace RedAnts.Ticketing.Features.Stats;

public interface IVisitorStatsReport
{
    Task<VisitorOverview> GetAsync(DateOnly from, DateOnly toExclusive);
}
