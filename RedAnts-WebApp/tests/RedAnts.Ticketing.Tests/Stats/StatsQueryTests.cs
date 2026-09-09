using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Features.Stats;
using RedAnts.Ticketing.Tests.Orders;
using Xunit;

namespace RedAnts.Ticketing.Tests.Stats;

public class GetSeasonStatsTests
{
    [Fact]
    public async Task Reads_the_season_with_its_events_and_start()
    {
        var reader = new StubSeasonVisitStats();
        var seasons = new StubSeasons();
        seasons.Seasons.Add(Season.FromPersistence(3, "2026/27", new DateOnly(2026, 8, 1), new DateOnly(2027, 5, 31), SeasonStatus.Open));
        var events = new StubEvents();
        events.Events.Add(StubEvents.InSeason(10, 3));
        events.Events.Add(StubEvents.InSeason(20, 4));

        var stats = await new GetSeasonStats.Handler(reader, seasons, events).HandleAsync(new GetSeasonStats.Query(3));

        Assert.Same(reader.Stats, stats);
        Assert.Equal(3, reader.LastCall!.Value.SeasonId);
        Assert.Equal([10], reader.LastCall.Value.EventIds);
        Assert.Equal(new DateTime(2026, 8, 1), reader.LastCall.Value.SeasonStart);
    }
}

public class GetSalesStatsTests
{
    [Fact]
    public async Task Reads_the_sales_of_the_season_events()
    {
        var reader = new StubSalesStats();
        var events = new StubEvents();
        events.Events.Add(StubEvents.InSeason(10, 3));

        var stats = await new GetSalesStats.Handler(reader, events).HandleAsync(new GetSalesStats.Query(3));

        Assert.Same(reader.Stats, stats);
        Assert.Equal(3, reader.LastCall!.Value.SeasonId);
        Assert.Equal([10], reader.LastCall.Value.EventIds);
    }
}

public class GetEventStatsTests
{
    [Fact]
    public async Task Reads_the_event_with_its_kickoff_and_yields_nothing_for_an_unknown_event()
    {
        var reader = new StubEventVisitStats();
        var events = new StubEvents();
        events.Events.Add(StubEvents.InSeason(10, 3));
        var handler = new GetEventStats.Handler(reader, events);

        var stats = await handler.HandleAsync(new GetEventStats.Query(10));
        var unknown = await handler.HandleAsync(new GetEventStats.Query(99));

        Assert.Same(reader.Stats, stats);
        Assert.Equal((10, new DateTime(2026, 10, 3, 18, 0, 0)), reader.LastCall);
        Assert.Null(unknown);
    }
}

public class GetVisitorStatsTests
{
    [Fact]
    public async Task Reads_the_visitor_overview_for_the_range()
    {
        var reader = new StubVisitorStats();
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 10, 1);

        var overview = await new GetVisitorStats.Handler(reader).HandleAsync(new GetVisitorStats.Query(from, to));

        Assert.Same(reader.Overview, overview);
        Assert.Equal((from, to), reader.LastCall);
    }
}
