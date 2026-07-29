using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Scanning;
using RedAnts.Infrastructure.Ticketing.Scanning;
using Xunit;

namespace RedAnts.Infrastructure.Tests.Scanning;

public class AdmissionRulesTests
{
    private const int Event = 100;
    private const int OtherEvent = 200;
    private const int ThirdEvent = 300;
    private const int Season = 5;
    private const int OtherSeason = 9;

    public static IEnumerable<object[]> ScannableTypes =>
    [
        [TicketType.EventTicket],
        [TicketType.SeasonSingle],
        [TicketType.SeasonPass],
        [TicketType.MemberCard]
    ];

    public static IEnumerable<object[]> SeasonScopedTypes =>
    [
        [TicketType.SeasonSingle],
        [TicketType.SeasonPass],
        [TicketType.MemberCard]
    ];

    public static IEnumerable<object[]> ScannableTypesBothModes =>
        from t in new[] { TicketType.EventTicket, TicketType.SeasonSingle, TicketType.SeasonPass, TicketType.MemberCard }
        from m in new[] { ScanMode.CheckIn, ScanMode.CheckOut }
        select new object[] { t, m };

    private static (int Scope, int? SeasonId) ScopeFor(TicketType type, int eventId = Event, int season = Season) =>
        type == TicketType.EventTicket ? (eventId, (int?)null) : (season, season);

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

    private static AdmissionEvaluation RunValid(
        TicketType type,
        ScanMode mode = ScanMode.CheckIn,
        int eventId = Event,
        TicketStatus status = TicketStatus.Valid,
        bool test = false,
        int? redeemedEventId = null,
        bool visitExists = false,
        bool visitInside = false)
    {
        var (scope, season) = ScopeFor(type, eventId);
        return Run(type: type, eventId: eventId, scopeId: scope, mode: mode, test: test, status: status,
            eventSeasonId: season, redeemedEventId: redeemedEventId,
            visitExists: visitExists, visitInside: visitInside);
    }

    private static void AssertReject(AdmissionEvaluation r, string reason)
    {
        Assert.Equal(AdmissionVerdict.Reject, r.Verdict);
        Assert.Equal(reason, r.Reason);
    }

    [Fact]
    public void EmptyUuid_IsTreatedAsScannerTest()
    {
        Assert.Equal(AdmissionVerdict.TestEmpty, Run(empty: true, ticketFound: false).Verdict);
    }

    [Theory]
    [MemberData(nameof(ScannableTypesBothModes))]
    public void UnknownTicket_IsRejected_ForEveryTypeAndMode(TicketType type, ScanMode mode)
    {
        var (scope, season) = ScopeFor(type);
        AssertReject(Run(type: type, scopeId: scope, mode: mode, eventSeasonId: season, ticketFound: false),
            AdmissionRules.UnknownTicket);
    }

    [Theory]
    [MemberData(nameof(ScannableTypes))]
    public void FreshCheckIn_IsAdmitted_ForEveryTicketType(TicketType type)
    {
        Assert.Equal(AdmissionVerdict.Admit, RunValid(type).Verdict);
    }

    [Theory]
    [MemberData(nameof(ScannableTypes))]
    public void AlreadyInside_CheckIn_IsRejected_ForEveryTicketType(TicketType type)
    {
        AssertReject(RunValid(type, ScanMode.CheckIn, visitExists: true, visitInside: true),
            AdmissionRules.AlreadyCheckedIn);
    }

    [Theory]
    [MemberData(nameof(ScannableTypes))]
    public void CheckOutWithoutCheckIn_IsRejected_ForEveryTicketType(TicketType type)
    {
        AssertReject(RunValid(type, ScanMode.CheckOut), AdmissionRules.NotCheckedIn);
    }

    [Theory]
    [MemberData(nameof(ScannableTypes))]
    public void CheckOutWhenAlreadyOutside_IsRejected_ForEveryTicketType(TicketType type)
    {
        AssertReject(RunValid(type, ScanMode.CheckOut, visitExists: true, visitInside: false),
            AdmissionRules.NotCheckedIn);
    }

    [Theory]
    [MemberData(nameof(ScannableTypes))]
    public void CheckOutWhenInside_IsAdmitted_ForEveryTicketType(TicketType type)
    {
        Assert.Equal(AdmissionVerdict.Admit,
            RunValid(type, ScanMode.CheckOut, visitExists: true, visitInside: true).Verdict);
    }

    [Theory]
    [MemberData(nameof(ScannableTypes))]
    public void ReCheckInAfterCheckOut_IsAdmitted_ForEveryTicketType(TicketType type)
    {
        Assert.Equal(AdmissionVerdict.Admit,
            RunValid(type, ScanMode.CheckIn, visitExists: true, visitInside: false).Verdict);
    }

    [Theory]
    [MemberData(nameof(ScannableTypesBothModes))]
    public void TestMode_ReportsTestWithoutSideEffects_ForEveryType(TicketType type, ScanMode mode)
    {
        Assert.Equal(AdmissionVerdict.TestTicket, RunValid(type, mode, test: true).Verdict);
    }

    [Theory]
    [MemberData(nameof(ScannableTypesBothModes))]
    public void BlockedTicket_IsRejected_ForEveryTypeAndMode(TicketType type, ScanMode mode)
    {
        AssertReject(RunValid(type, mode, status: TicketStatus.Blocked), AdmissionRules.Blocked);
    }

    [Theory]
    [MemberData(nameof(ScannableTypesBothModes))]
    public void CancelledTicket_IsRejected_ForEveryTypeAndMode(TicketType type, ScanMode mode)
    {
        AssertReject(RunValid(type, mode, status: TicketStatus.Cancelled), AdmissionRules.Cancelled);
    }

    [Theory]
    [MemberData(nameof(ScannableTypes))]
    public void TypeMismatch_IsRejected_ForEveryTicketType(TicketType type)
    {
        var wrong = type == TicketType.EventTicket ? TicketType.SeasonPass : TicketType.EventTicket;
        var (scope, season) = ScopeFor(type);
        AssertReject(Run(type: type, scopeId: scope, eventSeasonId: season, issuedType: wrong),
            AdmissionRules.RecordMismatch);
    }

    [Theory]
    [MemberData(nameof(ScannableTypes))]
    public void ScopeMismatch_IsRejected_ForEveryTicketType(TicketType type)
    {
        var (scope, season) = ScopeFor(type);
        AssertReject(Run(type: type, scopeId: scope, eventSeasonId: season, issuedScope: scope + 1),
            AdmissionRules.RecordMismatch);
    }

    [Theory]
    [MemberData(nameof(SeasonScopedTypes))]
    public void SeasonTicket_ForWrongSeason_IsRejected(TicketType type)
    {
        AssertReject(Run(type: type, scopeId: OtherSeason, eventSeasonId: Season), AdmissionRules.WrongSeason);
    }

    [Theory]
    [MemberData(nameof(SeasonScopedTypes))]
    public void SeasonTicket_WhenEventIsUnknown_IsRejected(TicketType type)
    {
        AssertReject(Run(type: type, scopeId: Season, eventSeasonId: null), AdmissionRules.UnknownEvent);
    }

    [Theory]
    [InlineData(Event, OtherEvent)]
    [InlineData(Event, ThirdEvent)]
    [InlineData(OtherEvent, Event)]
    [InlineData(ThirdEvent, Event)]
    public void EventTicket_ForWrongEvent_IsRejected(int eventId, int ticketEvent)
    {
        AssertReject(Run(type: TicketType.EventTicket, eventId: eventId, scopeId: ticketEvent),
            AdmissionRules.WrongEvent);
    }

    [Theory]
    [InlineData(Event)]
    [InlineData(OtherEvent)]
    [InlineData(ThirdEvent)]
    public void SeasonPass_IsAdmittedAtEveryEventOfItsSeason_WhenNotYetInside(int eventId)
    {
        Assert.Equal(AdmissionVerdict.Admit, RunValid(TicketType.SeasonPass, eventId: eventId).Verdict);
    }

    [Theory]
    [InlineData(Event)]
    [InlineData(OtherEvent)]
    [InlineData(ThirdEvent)]
    public void MemberCard_UsedAtOneGame_IsStillAdmittedAtOtherGames(int eventId)
    {
        Assert.Equal(AdmissionVerdict.Admit, RunValid(TicketType.MemberCard, eventId: eventId).Verdict);
    }

    [Fact]
    public void FlexTicket_NotYetRedeemed_IsAdmitted()
    {
        Assert.Equal(AdmissionVerdict.Admit, RunValid(TicketType.SeasonSingle, redeemedEventId: null).Verdict);
    }

    [Fact]
    public void FlexTicket_RedeemedAtThisEvent_AfterCheckOut_IsAdmitted()
    {
        Assert.Equal(AdmissionVerdict.Admit,
            RunValid(TicketType.SeasonSingle, redeemedEventId: Event, visitExists: true, visitInside: false).Verdict);
    }

    [Theory]
    [InlineData(OtherEvent)]
    [InlineData(ThirdEvent)]
    public void FlexTicket_RedeemedAtAnotherEvent_IsRejected(int redeemedAt)
    {
        AssertReject(RunValid(TicketType.SeasonSingle, eventId: Event, redeemedEventId: redeemedAt),
            AdmissionRules.FlexRedeemedElsewhere);
    }

    [Fact]
    public void FlexTicket_AlreadyInsideThisEvent_IsRejected()
    {
        AssertReject(
            RunValid(TicketType.SeasonSingle, redeemedEventId: Event, visitExists: true, visitInside: true),
            AdmissionRules.AlreadyCheckedIn);
    }

    [Theory]
    [InlineData(TicketType.EventTicket)]
    [InlineData(TicketType.SeasonPass)]
    [InlineData(TicketType.MemberCard)]
    public void RedeemedEventId_IsIgnored_ForNonFlexTickets(TicketType type)
    {
        Assert.Equal(AdmissionVerdict.Admit, RunValid(type, redeemedEventId: OtherEvent).Verdict);
    }

    [Fact]
    public void EventTicket_IgnoresEventSeasonId()
    {
        var r = Run(type: TicketType.EventTicket, eventId: Event, scopeId: Event, eventSeasonId: OtherSeason);
        Assert.Equal(AdmissionVerdict.Admit, r.Verdict);
    }

    [Fact]
    public void RecordMismatch_TakesPrecedenceOverBlockedStatus()
    {
        var r = Run(type: TicketType.EventTicket, scopeId: Event, issuedScope: OtherEvent, status: TicketStatus.Blocked);
        AssertReject(r, AdmissionRules.RecordMismatch);
    }

    [Fact]
    public void BlockedStatus_TakesPrecedenceOverWrongEvent()
    {
        var r = Run(type: TicketType.EventTicket, eventId: OtherEvent, scopeId: Event, status: TicketStatus.Blocked);
        AssertReject(r, AdmissionRules.Blocked);
    }

    [Fact]
    public void BlockedStatus_TakesPrecedenceOverWrongSeason()
    {
        var r = Run(type: TicketType.SeasonPass, scopeId: OtherSeason, eventSeasonId: Season, status: TicketStatus.Blocked);
        AssertReject(r, AdmissionRules.Blocked);
    }

    [Fact]
    public void TestMode_TakesPrecedenceOverWrongEvent()
    {
        var r = Run(type: TicketType.EventTicket, eventId: OtherEvent, scopeId: Event, test: true);
        Assert.Equal(AdmissionVerdict.TestTicket, r.Verdict);
    }

    [Fact]
    public void TestMode_TakesPrecedenceOverAlreadyCheckedIn()
    {
        Assert.Equal(AdmissionVerdict.TestTicket,
            RunValid(TicketType.EventTicket, test: true, visitExists: true, visitInside: true).Verdict);
    }

    [Fact]
    public void WrongEvent_TakesPrecedenceOverAlreadyCheckedIn()
    {
        var r = Run(type: TicketType.EventTicket, eventId: OtherEvent, scopeId: Event,
            visitExists: true, visitInside: true);
        AssertReject(r, AdmissionRules.WrongEvent);
    }

    [Fact]
    public void WrongSeason_TakesPrecedenceOverAlreadyCheckedIn()
    {
        var r = Run(type: TicketType.SeasonPass, scopeId: OtherSeason, eventSeasonId: Season,
            visitExists: true, visitInside: true);
        AssertReject(r, AdmissionRules.WrongSeason);
    }

    [Fact]
    public void WrongSeason_TakesPrecedenceOverFlexRedeemedElsewhere()
    {
        var r = Run(type: TicketType.SeasonSingle, eventId: Event, scopeId: OtherSeason, eventSeasonId: Season,
            redeemedEventId: OtherEvent);
        AssertReject(r, AdmissionRules.WrongSeason);
    }

    [Fact]
    public void FlexRedeemedElsewhere_TakesPrecedenceOverAlreadyCheckedIn()
    {
        var r = Run(type: TicketType.SeasonSingle, eventId: Event, scopeId: Season, eventSeasonId: Season,
            redeemedEventId: OtherEvent, visitExists: true, visitInside: true);
        AssertReject(r, AdmissionRules.FlexRedeemedElsewhere);
    }

    [Fact]
    public void UnknownTicket_TakesPrecedenceOverEverythingElse()
    {
        var r = Run(type: TicketType.SeasonSingle, eventId: Event, scopeId: OtherSeason, eventSeasonId: Season,
            ticketFound: false, redeemedEventId: OtherEvent, visitExists: true, visitInside: true);
        AssertReject(r, AdmissionRules.UnknownTicket);
    }

    [Theory]
    [InlineData(AdmissionRules.AlreadyCheckedIn, true)]
    [InlineData(AdmissionRules.NotCheckedIn, true)]
    [InlineData(AdmissionRules.UnknownTicket, false)]
    [InlineData(AdmissionRules.RecordMismatch, false)]
    [InlineData(AdmissionRules.Blocked, false)]
    [InlineData(AdmissionRules.Cancelled, false)]
    [InlineData(AdmissionRules.WrongEvent, false)]
    [InlineData(AdmissionRules.UnknownEvent, false)]
    [InlineData(AdmissionRules.WrongSeason, false)]
    [InlineData(AdmissionRules.FlexRedeemedElsewhere, false)]
    [InlineData(null, false)]
    public void CarriesHolder_OnlyForModeStageRejections(string? reason, bool expected)
    {
        Assert.Equal(expected, AdmissionRules.CarriesHolder(reason));
    }
}
