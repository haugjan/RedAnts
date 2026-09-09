using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.AdmissionWorkflow;
using Xunit;

namespace RedAnts.Ticketing.Tests.AdmissionWorkflow;

public class FreeEntryHandlerTests
{
    private const int EventId = 10;

    private readonly InMemoryFreeEntries _freeEntries = new();
    private readonly StubOccupancy _occupancy = new();

    private GrantFreeEntry.Handler Grant => new(_freeEntries, _occupancy);
    private RevokeFreeEntry.Handler Revoke => new(_freeEntries, _occupancy);

    private static FreeEntryQuota QuotaOf(FreeEntryType type, int quota) =>
        new(new Dictionary<FreeEntryType, int?> { [type] = quota }, new Dictionary<FreeEntryType, int>());

    [Fact]
    public async Task Granting_stores_an_inside_entry_and_reports_the_type()
    {
        var outcome = await Grant.HandleAsync(new GrantFreeEntry.Command(EventId, FreeEntryType.Player, "Anna"));

        Assert.Equal(AdmissionOutcome.CheckedIn, outcome.Outcome);
        Assert.Equal(TicketType.FreeEntry, outcome.Type);
        Assert.Equal(FreeEntryType.Player.DisplayName(), outcome.Reference);
        var entry = Assert.Single(_freeEntries.Stored);
        Assert.True(entry.IsInside);
        Assert.False(entry.IsNew);
        Assert.Equal("Anna", Assert.Single(entry.Logs).ScannedBy);
    }

    [Fact]
    public async Task Exhausted_quota_rejects_the_grant()
    {
        _freeEntries.Quota = QuotaOf(FreeEntryType.Staff, 2);
        await Grant.HandleAsync(new GrantFreeEntry.Command(EventId, FreeEntryType.Staff, null));
        await Grant.HandleAsync(new GrantFreeEntry.Command(EventId, FreeEntryType.Staff, null));

        var outcome = await Grant.HandleAsync(new GrantFreeEntry.Command(EventId, FreeEntryType.Staff, null));

        Assert.Equal(AdmissionOutcome.Rejected, outcome.Outcome);
        Assert.Equal($"Kontingent für {FreeEntryType.Staff.DisplayName()} erschöpft (2/2).", outcome.Reason);
        Assert.Equal(2, _freeEntries.Stored.Count);
    }

    [Theory]
    [InlineData(FreeEntryType.SwissUnihockeyFreeCard)]
    [InlineData(FreeEntryType.Child)]
    public async Task Full_hall_rejects_free_cards_and_children(FreeEntryType type)
    {
        _occupancy.Current = new Occupancy(120, 120);

        var outcome = await Grant.HandleAsync(new GrantFreeEntry.Command(EventId, type, null));

        Assert.Equal(AdmissionOutcome.Rejected, outcome.Outcome);
        Assert.Equal($"Halle voll — kein Gratiseintritt ({type.DisplayName()}) mehr.", outcome.Reason);
        Assert.Empty(_freeEntries.Stored);
    }

    [Fact]
    public async Task Full_hall_still_admits_players()
    {
        _occupancy.Current = new Occupancy(120, 120);

        var outcome = await Grant.HandleAsync(new GrantFreeEntry.Command(EventId, FreeEntryType.Player, null));

        Assert.Equal(AdmissionOutcome.CheckedIn, outcome.Outcome);
    }

    [Fact]
    public async Task Revoking_without_anyone_inside_is_rejected()
    {
        var outcome = await Revoke.HandleAsync(new RevokeFreeEntry.Command(EventId, FreeEntryType.Helper, "Anna"));

        Assert.Equal(AdmissionOutcome.Rejected, outcome.Outcome);
        Assert.Equal($"Kein freier Einlass ({FreeEntryType.Helper.DisplayName()}) zum Auschecken.", outcome.Reason);
    }

    [Fact]
    public async Task Revoking_checks_out_the_latest_entry_of_that_type()
    {
        await Grant.HandleAsync(new GrantFreeEntry.Command(EventId, FreeEntryType.Helper, "Anna"));
        await Grant.HandleAsync(new GrantFreeEntry.Command(EventId, FreeEntryType.Official, "Anna"));

        var outcome = await Revoke.HandleAsync(new RevokeFreeEntry.Command(EventId, FreeEntryType.Helper, "Beat"));

        Assert.Equal(AdmissionOutcome.CheckedOut, outcome.Outcome);
        Assert.Equal(FreeEntryType.Helper.DisplayName(), outcome.Reference);
        var helper = Assert.Single(_freeEntries.Stored, e => e.Type == FreeEntryType.Helper);
        Assert.False(helper.IsInside);
        Assert.Equal(VisitLogType.CheckOut, helper.Logs.Last().Type);
        Assert.True(Assert.Single(_freeEntries.Stored, e => e.Type == FreeEntryType.Official).IsInside);
    }
}
