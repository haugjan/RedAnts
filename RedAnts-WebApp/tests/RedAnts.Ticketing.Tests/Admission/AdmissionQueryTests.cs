using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Tests.Catalog;
using RedAnts.Ticketing.Tests.Orders;
using Xunit;

namespace RedAnts.Ticketing.Tests.Admission;

public class GetFreeEntriesTests
{
    [Fact]
    public async Task Returns_the_rows_of_the_event_and_nothing_for_a_missing_event()
    {
        var reader = new StubFreeEntryList();
        reader.Rows.Add(new FreeEntryRow(Guid.NewGuid(), SwissTime.Timestamp, "Anna", true, FreeEntryType.Player));
        var handler = new GetFreeEntries.Handler(reader);

        var rows = await handler.HandleAsync(new GetFreeEntries.Query(10));
        var none = await handler.HandleAsync(new GetFreeEntries.Query(0));

        Assert.Equal(10, reader.LastEventId);
        Assert.Single(rows);
        Assert.Empty(none);
    }
}

public class GetVisitsTests
{
    [Fact]
    public async Task Returns_the_visit_entries_of_the_ticket()
    {
        var reader = new StubVisitLog();
        var uuid = Guid.NewGuid();
        var entry = new TicketVisitEntry(1, 10, "Spiel", new DateOnly(2026, 10, 3), true,
            [new TicketVisitScan(VisitLogType.CheckIn, SwissTime.Timestamp, "Anna")]);
        reader.Visits[uuid] = [entry];

        var visits = await new GetVisits.Handler(reader).HandleAsync(new GetVisits.Query(uuid));

        Assert.Same(entry, Assert.Single(visits));
        Assert.Empty(await new GetVisits.Handler(reader).HandleAsync(new GetVisits.Query(Guid.NewGuid())));
    }
}

public class GetEventAdmissionReportTests
{
    [Fact]
    public async Task Returns_the_counts_per_event()
    {
        var reader = new StubEventAdmission();
        reader.Counts[10] = new EventAdmissionCounts(50, 40, 3, 5, 2, 1);

        var counts = await new GetEventAdmissionReport.Handler(reader).HandleAsync(new GetEventAdmissionReport.Query());

        Assert.Equal(51, counts[10].TotalRedeemed);
    }
}

public class DeleteFreeEntryTests
{
    [Fact]
    public async Task Deletes_the_entry_through_the_repository()
    {
        var freeEntries = new InMemoryFreeEntries();
        var entry = Domain.Admission.FreeEntry.Grant(10, FreeEntryType.Staff, "Anna", SwissTime.Timestamp);
        await freeEntries.SaveAsync(entry);

        await new DeleteFreeEntry.Handler(freeEntries).HandleAsync(new DeleteFreeEntry.Command(entry.Uuid));

        Assert.Empty(freeEntries.Stored);
    }
}

public class ResolveScannedCodeTests
{
    private readonly VerifyingTicketTokens _tokens = new();

    private ResolveScannedCode.Handler Handler => new(_tokens);

    [Fact]
    public async Task A_signed_token_yields_its_data()
    {
        var data = new TicketTokenData(TicketType.EventTicket, Guid.NewGuid(), 10, SwissTime.Timestamp);
        _tokens.Tokens["signed"] = data;

        var resolved = await Handler.HandleAsync(new ResolveScannedCode.Query("signed"));

        Assert.Equal(data, resolved.Token);
        Assert.Null(resolved.ShortCode);
    }

    [Fact]
    public async Task A_short_token_yields_its_code_and_garbage_yields_nothing()
    {
        _tokens.ShortCodes["short"] = "a1b2c3d4";

        var resolved = await Handler.HandleAsync(new ResolveScannedCode.Query("short"));
        var garbage = await Handler.HandleAsync(new ResolveScannedCode.Query("nonsense"));

        Assert.Equal((null, "a1b2c3d4"), (resolved.Token, resolved.ShortCode));
        Assert.Equal((null, null), (garbage.Token, garbage.ShortCode));
    }
}

public class GetEventsForScanningTests
{
    private readonly StubEvents _events = new();
    private readonly StubVenues _venues = new();

    private GetEventsForScanning.Handler Handler => new(_events, _venues);

    public GetEventsForScanningTests()
    {
        _venues.Venues.Add(Venue.FromPersistence(1, "Eulachhalle", null, null, null));
        _events.Events.Add(StubEvents.InSeason(10, 3));
        _events.Events.Add(StubEvents.InSeason(11, 3));
    }

    [Fact]
    public async Task Lists_the_upcoming_events_with_their_venue_names()
    {
        var events = await Handler.HandleAsync(new GetEventsForScanning.Query());

        Assert.Equal([10, 11], events.Select(e => e.Id));
        Assert.All(events, e => Assert.Equal("Eulachhalle", e.VenueName));
        Assert.Equal(EventStatus.Open, events[0].Status);
    }

    [Fact]
    public async Task Restricts_to_the_allowed_events_of_a_helper()
    {
        var events = await Handler.HandleAsync(new GetEventsForScanning.Query([11]));

        Assert.Equal(11, Assert.Single(events).Id);
    }
}
