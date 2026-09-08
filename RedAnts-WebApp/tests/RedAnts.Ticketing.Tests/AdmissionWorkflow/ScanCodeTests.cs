using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.AdmissionWorkflow;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Tickets;
using Xunit;

namespace RedAnts.Ticketing.Tests.AdmissionWorkflow;

public class ScanCodeTests
{
    private const int EventId = 10;

    private readonly StubIssuedTickets _tickets = new();
    private readonly StubAdmissionFacts _facts = new();
    private readonly InMemoryAdmissions _admissions = new();
    private readonly RecordingRedemptions _redemptions = new();
    private readonly StubOccupancy _occupancy = new();

    private ScanCode.Handler Handler => new(_tickets, _occupancy, new TicketScanning(_facts, _admissions, _redemptions, _occupancy));

    [Theory]
    [InlineData("abc")]
    [InlineData("zzzzzzzz")]
    [InlineData("")]
    public async Task A_code_that_is_not_eight_hex_characters_is_rejected(string code)
    {
        var outcome = await Handler.HandleAsync(new ScanCode.Command(EventId, code, ScanMode.CheckIn, "Anna"));

        Assert.Equal(AdmissionOutcome.Rejected, outcome.Outcome);
        Assert.Equal(ScanCode.BadCode, outcome.Reason);
        Assert.Null(outcome.Type);
        Assert.Equal(code.ToUpperInvariant(), outcome.Reference);
    }

    [Fact]
    public async Task An_unknown_code_is_rejected_with_the_upper_case_code()
    {
        var outcome = await Handler.HandleAsync(new ScanCode.Command(EventId, " 0a1b 2c3d ", ScanMode.CheckIn, "Anna"));

        Assert.Equal(AdmissionOutcome.Rejected, outcome.Outcome);
        Assert.Equal(ScanCode.UnknownCode, outcome.Reason);
        Assert.Equal("0A1B2C3D", outcome.Reference);
    }

    [Fact]
    public async Task A_resolved_code_scans_the_ticket()
    {
        var uuid = Guid.NewGuid();
        var issued = new IssuedTicket(TicketType.EventTicket, uuid, EventId, TicketCategory.Adult, TicketStatus.Valid, SwissTime.Timestamp, null,
            BuyerName: "Max Muster", CategoryName: "Erwachsene");
        _tickets.Tickets[uuid] = issued;
        _facts.Facts[uuid] = new AdmissionFacts(issued, null, null, false, false, null, null);

        var outcome = await Handler.HandleAsync(new ScanCode.Command(EventId, uuid.ToString("N")[..8].ToUpperInvariant(), ScanMode.CheckIn, "Anna"));

        Assert.Equal(AdmissionOutcome.CheckedIn, outcome.Outcome);
        Assert.Equal(TicketType.EventTicket, outcome.Type);
        Assert.Equal(1, _admissions.Stored(EventId, uuid)!.InsideCount);
    }
}
