using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Scanning;
using RedAnts.Infrastructure.Ticketing.Scanning;
using Xunit;

namespace RedAnts.Infrastructure.Tests.Scanning;

public class AdmissionRulesTests
{
    private const int Event = 100;
    private const int OtherEvent = 200;
    private const int Season = 5;
    private const int OtherSeason = 9;

    private static AdmissionEvaluation Run(
        TicketType type = TicketType.EventTicket,
        int eventId = Event,
        int scopeId = Event,
        ScanMode mode = ScanMode.CheckIn,
        bool test = false,
        bool empty = false,
        TicketStatus status = TicketStatus.Valid,
        bool ticketFound = true,
        TicketType? issuedType = null,
        int? issuedScope = null,
        int? eventSeasonId = null,
        int? redeemedEventId = null,
        bool visitExists = false,
        bool visitInside = false)
    {
        var facts = ticketFound
            ? new ScannedTicketFacts(issuedType ?? type, issuedScope ?? scopeId, status)
            : null;

        return AdmissionRules.Evaluate(
            eventId, type, scopeId, mode, test, empty, facts,
            eventSeasonId, redeemedEventId, visitExists, visitInside);
    }

    [Fact]
    public void EmptyUuid_IsTreatedAsScannerTest()
    {
        var result = Run(empty: true, ticketFound: false);
        Assert.Equal(AdmissionVerdict.TestEmpty, result.Verdict);
    }

    [Fact]
    public void UnknownTicket_IsRejected()
    {
        var result = Run(ticketFound: false);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.UnknownTicket, result.Reason);
    }

    [Fact]
    public void TicketTypeMismatch_IsRejected()
    {
        var result = Run(type: TicketType.EventTicket, issuedType: TicketType.SeasonPass);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.RecordMismatch, result.Reason);
    }

    [Fact]
    public void TicketScopeMismatch_IsRejected()
    {
        var result = Run(scopeId: Event, issuedScope: OtherEvent);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.RecordMismatch, result.Reason);
    }

    [Fact]
    public void BlockedTicket_IsRejected()
    {
        var result = Run(status: TicketStatus.Blocked);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.Blocked, result.Reason);
    }

    [Fact]
    public void CancelledTicket_IsRejected()
    {
        var result = Run(status: TicketStatus.Cancelled);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.Cancelled, result.Reason);
    }

    [Fact]
    public void BlockedTicket_IsRejected_EvenInTestMode()
    {
        var result = Run(status: TicketStatus.Blocked, test: true);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.Blocked, result.Reason);
    }

    [Fact]
    public void TestMode_WithValidTicket_ReportsTestWithoutAdmitting()
    {
        var result = Run(test: true);
        Assert.Equal(AdmissionVerdict.TestTicket, result.Verdict);
    }

    [Fact]
    public void EventTicket_ForWrongEvent_IsRejected()
    {
        var result = Run(type: TicketType.EventTicket, eventId: OtherEvent, scopeId: Event);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.WrongEvent, result.Reason);
    }

    [Fact]
    public void EventTicket_ForCorrectEvent_FreshCheckIn_IsAdmitted()
    {
        var result = Run(type: TicketType.EventTicket, eventId: Event, scopeId: Event);
        Assert.Equal(AdmissionVerdict.Admit, result.Verdict);
    }

    [Fact]
    public void SeasonTicket_ForWrongSeason_IsRejected()
    {
        var result = Run(type: TicketType.SeasonPass, scopeId: OtherSeason, eventSeasonId: Season);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.WrongSeason, result.Reason);
    }

    [Fact]
    public void SeasonTicket_WhenEventIsUnknown_IsRejected()
    {
        var result = Run(type: TicketType.SeasonPass, scopeId: Season, eventSeasonId: null);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.UnknownEvent, result.Reason);
    }

    [Fact]
    public void SeasonPass_ForCorrectSeason_FreshCheckIn_IsAdmitted()
    {
        var result = Run(type: TicketType.SeasonPass, scopeId: Season, eventSeasonId: Season);
        Assert.Equal(AdmissionVerdict.Admit, result.Verdict);
    }

    [Fact]
    public void FlexTicket_RedeemedAtAnotherEvent_IsRejected()
    {
        var result = Run(type: TicketType.SeasonSingle, scopeId: Season, eventSeasonId: Season,
            eventId: Event, redeemedEventId: OtherEvent);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.FlexRedeemedElsewhere, result.Reason);
    }

    [Fact]
    public void FlexTicket_RedeemedAtThisEvent_AfterCheckOut_IsAdmitted()
    {
        var result = Run(type: TicketType.SeasonSingle, scopeId: Season, eventSeasonId: Season,
            eventId: Event, redeemedEventId: Event, visitExists: true, visitInside: false);
        Assert.Equal(AdmissionVerdict.Admit, result.Verdict);
    }

    [Fact]
    public void FlexTicket_NotYetRedeemed_IsAdmitted()
    {
        var result = Run(type: TicketType.SeasonSingle, scopeId: Season, eventSeasonId: Season,
            eventId: Event, redeemedEventId: null);
        Assert.Equal(AdmissionVerdict.Admit, result.Verdict);
    }

    [Fact]
    public void GameTicket_AlreadyCheckedIn_IsRejected()
    {
        var result = Run(type: TicketType.EventTicket, visitExists: true, visitInside: true);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.AlreadyCheckedIn, result.Reason);
    }

    [Fact]
    public void FlexTicket_AlreadyInsideThisEvent_IsRejected()
    {
        var result = Run(type: TicketType.SeasonSingle, scopeId: Season, eventSeasonId: Season,
            eventId: Event, redeemedEventId: Event, visitExists: true, visitInside: true);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.AlreadyCheckedIn, result.Reason);
    }

    [Fact]
    public void MemberCard_AlreadyUsedForThisGame_IsRejected()
    {
        var result = Run(type: TicketType.MemberCard, scopeId: Season, eventSeasonId: Season,
            visitExists: true, visitInside: true);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.AlreadyCheckedIn, result.Reason);
    }

    [Fact]
    public void SeasonPass_AlreadyUsedForThisGame_IsRejected()
    {
        var result = Run(type: TicketType.SeasonPass, scopeId: Season, eventSeasonId: Season,
            visitExists: true, visitInside: true);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.AlreadyCheckedIn, result.Reason);
    }

    [Fact]
    public void MemberCard_UsedAtAnotherGame_IsStillAdmittedHere()
    {
        var result = Run(type: TicketType.MemberCard, scopeId: Season, eventSeasonId: Season,
            eventId: OtherEvent, visitExists: false, visitInside: false);
        Assert.Equal(AdmissionVerdict.Admit, result.Verdict);
    }

    [Fact]
    public void CheckOut_WithoutCheckIn_IsRejected()
    {
        var result = Run(mode: ScanMode.CheckOut, visitExists: false, visitInside: false);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.NotCheckedIn, result.Reason);
    }

    [Fact]
    public void CheckOut_WhenAlreadyOutside_IsRejected()
    {
        var result = Run(mode: ScanMode.CheckOut, visitExists: true, visitInside: false);
        Assert.Equal(AdmissionVerdict.Reject, result.Verdict);
        Assert.Equal(AdmissionRules.NotCheckedIn, result.Reason);
    }

    [Fact]
    public void CheckOut_WhenInside_IsAdmitted()
    {
        var result = Run(mode: ScanMode.CheckOut, visitExists: true, visitInside: true);
        Assert.Equal(AdmissionVerdict.Admit, result.Verdict);
    }

    [Fact]
    public void ReCheckIn_AfterCheckOut_IsAdmitted()
    {
        var result = Run(mode: ScanMode.CheckIn, visitExists: true, visitInside: false);
        Assert.Equal(AdmissionVerdict.Admit, result.Verdict);
    }

    [Theory]
    [InlineData(AdmissionRules.AlreadyCheckedIn, true)]
    [InlineData(AdmissionRules.NotCheckedIn, true)]
    [InlineData(AdmissionRules.UnknownTicket, false)]
    [InlineData(AdmissionRules.WrongEvent, false)]
    [InlineData(AdmissionRules.WrongSeason, false)]
    [InlineData(AdmissionRules.FlexRedeemedElsewhere, false)]
    [InlineData(null, false)]
    public void CarriesHolder_OnlyForModeStageRejections(string? reason, bool expected)
    {
        Assert.Equal(expected, AdmissionRules.CarriesHolder(reason));
    }
}
