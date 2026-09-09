using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Stats;

namespace RedAnts.Ticketing.Tests.Stats;

internal sealed class StubSeasons : ISeasons
{
    public List<Season> Seasons { get; } = [];

    public Task<IReadOnlyList<Season>> GetAllAsync() => Task.FromResult<IReadOnlyList<Season>>(Seasons);

    public Task<IReadOnlyList<Season>> GetPublicOpenAsync() => Task.FromResult<IReadOnlyList<Season>>(Seasons);

    public Task<Season?> FindByIdAsync(int id) => Task.FromResult(Seasons.FirstOrDefault(s => s.Id == id));
}

internal sealed class StubSeasonVisitStats : ISeasonVisitStatsReader
{
    public SeasonVisitStats Stats { get; } = new(0, 0, new Dictionary<int, int>(), [], [], [], [], new FlexFunnel(0, 0, 0, 0, 0),
        new PassUtilization(0, 0, 0, []), new MemberUsage(0, 0), []);
    public (int SeasonId, IReadOnlyCollection<int> EventIds, DateTime SeasonStart)? LastCall { get; private set; }

    public Task<SeasonVisitStats> GetAsync(int seasonId, IReadOnlyCollection<int> eventIds, DateTime seasonStartSwiss)
    {
        LastCall = (seasonId, eventIds, seasonStartSwiss);
        return Task.FromResult(Stats);
    }
}

internal sealed class StubSalesStats : ISalesStatsReader
{
    public SalesStats Stats { get; } = new();
    public (int SeasonId, IReadOnlyCollection<int> EventIds)? LastCall { get; private set; }

    public Task<SalesStats> GetSeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds)
    {
        LastCall = (seasonId, eventIds);
        return Task.FromResult(Stats);
    }
}

internal sealed class StubEventVisitStats : IEventVisitStatsReader
{
    public EventVisitStats Stats { get; } = new(0, 0, null, null, null, 0, 0, 0, 0, null, [], [], []);
    public (int EventId, DateTime Kickoff)? LastCall { get; private set; }

    public Task<EventVisitStats> GetAsync(int eventId, DateTime kickoffSwiss)
    {
        LastCall = (eventId, kickoffSwiss);
        return Task.FromResult(Stats);
    }
}

internal sealed class StubVisitorStats : IVisitorStatsReader
{
    public VisitorOverview Overview { get; } = new(0, 0, 0, StatBucket.Day, [], []);
    public (DateOnly From, DateOnly ToExclusive)? LastCall { get; private set; }

    public Task<VisitorOverview> GetAsync(DateOnly from, DateOnly toExclusive)
    {
        LastCall = (from, toExclusive);
        return Task.FromResult(Overview);
    }
}
