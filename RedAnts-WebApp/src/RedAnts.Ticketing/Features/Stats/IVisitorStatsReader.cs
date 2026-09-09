namespace RedAnts.Ticketing.Features.Stats;

public interface IVisitorStatsReader
{
    Task<VisitorOverview> GetAsync(DateOnly from, DateOnly toExclusive);
}
