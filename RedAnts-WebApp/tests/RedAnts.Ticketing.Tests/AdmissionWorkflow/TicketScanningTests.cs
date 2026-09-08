using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.AdmissionWorkflow;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Tickets;
using Xunit;

namespace RedAnts.Ticketing.Tests.AdmissionWorkflow;

public class TicketScanningTests
{
    private const int EventId = 10;
    private const int SeasonId = 3;

    private readonly StubAdmissionFacts _facts = new();
    private readonly InMemoryAdmissions _admissions = new();
    private readonly RecordingRedemptions _redemptions = new();
    private readonly StubOccupancy _occupancy = new();

    private TicketScanning Scanning => new(_facts, _admissions, _redemptions, _occupancy);

    private Guid Issue(TicketType type, int scopeId, TicketStatus status = TicketStatus.Valid, int admissions = 1,
        string? buyerName = "Max Muster", string? holderName = null, DateOnly? birthday = null, int? redeemedEventId = null)
    {
        var uuid = Guid.NewGuid();
        var issued = new IssuedTicket(type, uuid, scopeId, TicketCategory.Adult, status, SwissTime.Timestamp, holderName,
            Birthday: birthday, BuyerName: buyerName, CategoryName: "Erwachsene", Admissions: admissions);
        var seasonId = type == TicketType.EventTicket ? (int?)null : SeasonId;
        _facts.Facts[uuid] = new AdmissionFacts(issued, seasonId, redeemedEventId, false, false, null, null);
        return uuid;
    }

    private Task<ScanOutcome> ScanAsync(TicketType type, Guid uuid, int scopeId, ScanMode mode = ScanMode.CheckIn, string? by = "Anna", bool test = false) =>
        Scanning.ScanAsync(EventId, type, uuid, scopeId, mode, by, test);

    [Fact]
    public async Task Empty_uuid_is_a_scanner_test_without_reading_anything()
    {
        var outcome = await ScanAsync(TicketType.EventTicket, Guid.Empty, EventId);

        Assert.Equal(AdmissionOutcome.Test, outcome.Outcome);
        Assert.Equal("TEST", outcome.Reference);
        Assert.Equal("Scanner-Test", outcome.CategoryLabel);
        Assert.Equal(0, _facts.Reads);
        Assert.Equal(0, _admissions.Saves);
    }

    [Fact]
    public async Task Unknown_ticket_is_rejected_without_holder()
    {
        var outcome = await ScanAsync(TicketType.EventTicket, Guid.NewGuid(), EventId);

        Assert.Equal(AdmissionOutcome.Rejected, outcome.Outcome);
        Assert.Equal(AdmissionEvaluator.UnknownTicket, outcome.Reason);
        Assert.Null(outcome.Holder);
        Assert.Equal(0, _admissions.Saves);
    }

    [Fact]
    public async Task Event_ticket_check_in_creates_a_visit_and_marks_the_ticket_redeemed()
    {
        var uuid = Issue(TicketType.EventTicket, EventId);

        var outcome = await ScanAsync(TicketType.EventTicket, uuid, EventId);

        Assert.Equal(AdmissionOutcome.CheckedIn, outcome.Outcome);
        Assert.Equal(uuid.ToString("N")[..8].ToUpperInvariant(), outcome.Reference);
        Assert.Equal("Erwachsene", outcome.CategoryLabel);
        Assert.Equal("Max Muster", outcome.Holder);
        Assert.Null(outcome.AdmissionsUsed);
        var stored = _admissions.Stored(EventId, uuid);
        Assert.NotNull(stored);
        Assert.Equal(1, stored!.InsideCount);
        Assert.Single(stored.Visits);
        Assert.False(stored.Visits[0].IsNew);
        Assert.Contains((TicketType.EventTicket, uuid, EventId), _redemptions.Marked);
    }

    [Fact]
    public async Task Flex_ticket_check_in_marks_it_redeemed_but_a_season_pass_is_not()
    {
        var flex = Issue(TicketType.SeasonSingle, SeasonId);
        var pass = Issue(TicketType.SeasonPass, SeasonId);

        await ScanAsync(TicketType.SeasonSingle, flex, SeasonId);
        await ScanAsync(TicketType.SeasonPass, pass, SeasonId);

        Assert.Contains((TicketType.SeasonSingle, flex, EventId), _redemptions.Marked);
        Assert.DoesNotContain(_redemptions.Marked, m => m.Uuid == pass);
    }

    [Fact]
    public async Task Second_check_in_is_rejected_with_the_prior_scan()
    {
        var uuid = Issue(TicketType.EventTicket, EventId);
        await ScanAsync(TicketType.EventTicket, uuid, EventId, by: "Anna");

        var outcome = await ScanAsync(TicketType.EventTicket, uuid, EventId, by: "Beat");

        Assert.Equal(AdmissionOutcome.Rejected, outcome.Outcome);
        Assert.Equal(AdmissionEvaluator.AlreadyCheckedIn, outcome.Reason);
        Assert.Equal("Anna", outcome.PriorBy);
        Assert.NotNull(outcome.PriorAt);
        Assert.Equal("Max Muster", outcome.Holder);
        Assert.Equal(1, _admissions.Stored(EventId, uuid)!.InsideCount);
    }

    [Fact]
    public async Task Member_card_admits_up_to_its_cap_and_then_lists_all_prior_scans()
    {
        var uuid = Issue(TicketType.MemberCard, SeasonId, admissions: 3, holderName: "Lea Meier", birthday: new DateOnly(2000, 1, 1));

        var first = await ScanAsync(TicketType.MemberCard, uuid, SeasonId, by: "A");
        var second = await ScanAsync(TicketType.MemberCard, uuid, SeasonId, by: "B");
        var third = await ScanAsync(TicketType.MemberCard, uuid, SeasonId, by: "C");
        var fourth = await ScanAsync(TicketType.MemberCard, uuid, SeasonId, by: "D");

        Assert.Equal((1, 3), (first.AdmissionsUsed, first.AdmissionCap));
        Assert.Equal((2, 3), (second.AdmissionsUsed, second.AdmissionCap));
        Assert.Equal((3, 3), (third.AdmissionsUsed, third.AdmissionCap));
        Assert.Equal(AdmissionOutcome.Rejected, fourth.Outcome);
        Assert.Equal(AdmissionEvaluator.AllAdmissionsUsed, fourth.Reason);
        Assert.Equal(3, fourth.Priors!.Count);
        Assert.Equal(["A", "B", "C"], fourth.Priors.Select(p => p.By).ToArray());
        Assert.Equal("C", fourth.PriorBy);
        Assert.Equal(3, fourth.AdmissionsUsed);
        Assert.StartsWith("Lea Meier (", fourth.Holder);
        Assert.Equal(3, _admissions.Stored(EventId, uuid)!.Visits.Count);
    }

    [Fact]
    public async Task Check_out_after_check_in_leaves_the_hall()
    {
        var uuid = Issue(TicketType.EventTicket, EventId);
        await ScanAsync(TicketType.EventTicket, uuid, EventId);

        var outcome = await ScanAsync(TicketType.EventTicket, uuid, EventId, ScanMode.CheckOut);

        Assert.Equal(AdmissionOutcome.CheckedOut, outcome.Outcome);
        Assert.Equal(0, _admissions.Stored(EventId, uuid)!.InsideCount);
        Assert.Single(_admissions.Stored(EventId, uuid)!.Visits);
    }

    [Fact]
    public async Task Check_out_without_check_in_is_rejected()
    {
        var uuid = Issue(TicketType.EventTicket, EventId);

        var outcome = await ScanAsync(TicketType.EventTicket, uuid, EventId, ScanMode.CheckOut);

        Assert.Equal(AdmissionOutcome.Rejected, outcome.Outcome);
        Assert.Equal(AdmissionEvaluator.NotCheckedIn, outcome.Reason);
        Assert.Equal("Max Muster", outcome.Holder);
    }

    [Fact]
    public async Task Ticket_for_another_event_is_rejected_without_holder()
    {
        var uuid = Issue(TicketType.EventTicket, 99);

        var outcome = await ScanAsync(TicketType.EventTicket, uuid, 99);

        Assert.Equal(AdmissionOutcome.Rejected, outcome.Outcome);
        Assert.Equal(AdmissionEvaluator.WrongEvent, outcome.Reason);
        Assert.Null(outcome.Holder);
        Assert.Null(outcome.CategoryLabel);
        Assert.Equal(0, _admissions.Saves);
    }

    [Fact]
    public async Task Test_mode_reports_the_ticket_without_side_effects()
    {
        var uuid = Issue(TicketType.EventTicket, EventId);

        var outcome = await ScanAsync(TicketType.EventTicket, uuid, EventId, test: true);

        Assert.Equal(AdmissionOutcome.Test, outcome.Outcome);
        Assert.Equal("Max Muster", outcome.Holder);
        Assert.Equal(0, _admissions.Saves);
        Assert.Empty(_redemptions.Marked);
    }

    [Fact]
    public async Task Blocked_ticket_is_rejected()
    {
        var uuid = Issue(TicketType.EventTicket, EventId, status: TicketStatus.Blocked);

        var outcome = await ScanAsync(TicketType.EventTicket, uuid, EventId);

        Assert.Equal(AdmissionEvaluator.Blocked, outcome.Reason);
        Assert.Equal(0, _admissions.Saves);
    }
}
