using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using Xunit;

namespace RedAnts.Ticketing.Tests.Admission;

public class AdmissionTests
{
    private const int Event = 100;
    private static readonly Guid Ticket = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 18, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void None_rejects_a_missing_event(int eventId) =>
        Assert.Throws<DomainException>(() => Domain.Admission.Admission.None(eventId, TicketType.EventTicket, Ticket));

    [Fact]
    public void None_rejects_free_entry_as_ticket_type() =>
        Assert.Throws<DomainException>(() => Domain.Admission.Admission.None(Event, TicketType.FreeEntry, Ticket));

    [Fact]
    public void First_check_in_creates_one_new_visit_with_origin_and_one_check_in_log()
    {
        var admission = Domain.Admission.Admission.None(Event, TicketType.EventTicket, Ticket);
        var originCard = Guid.NewGuid().ToString();

        var visit = admission.CheckIn(1, "Kasse", Now, (int)TicketType.SeasonPass, originCard);

        Assert.Single(admission.Visits);
        Assert.Same(visit, admission.Visits[0]);
        Assert.True(visit.IsNew);
        Assert.True(visit.IsInside);
        Assert.True(visit.Changed);
        Assert.Equal(Now, visit.CreatedAt);
        Assert.Equal((int)TicketType.SeasonPass, visit.OriginType);
        Assert.Equal(originCard, visit.OriginCardUuid);
        var log = Assert.Single(visit.NewLogs);
        Assert.Equal(VisitLogType.CheckIn, log.Type);
        Assert.Equal(Now, log.OccurredAt);
        Assert.Equal("Kasse", log.ScannedBy);
        Assert.Equal(1, admission.InsideCount);
    }

    [Fact]
    public void Check_in_while_inside_is_already_checked_in_for_a_single_admission()
    {
        var admission = Domain.Admission.Admission.None(Event, TicketType.SeasonPass, Ticket);
        admission.CheckIn(1, null, Now);

        var ex = Assert.Throws<DomainException>(() => admission.CheckIn(1, null, Now.AddMinutes(1)));

        Assert.Equal(AdmissionEvaluator.AlreadyCheckedIn, ex.Message);
    }

    [Fact]
    public void Check_in_beyond_the_cap_is_all_admissions_used_for_member_cards()
    {
        var admission = Domain.Admission.Admission.None(Event, TicketType.MemberCard, Ticket);
        admission.CheckIn(2, null, Now);
        admission.CheckIn(2, null, Now.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() => admission.CheckIn(2, null, Now.AddMinutes(2)));

        Assert.Equal(AdmissionEvaluator.AllAdmissionsUsed, ex.Message);
        Assert.Equal(2, admission.InsideCount);
    }

    [Fact]
    public void Check_out_when_nobody_is_inside_is_not_checked_in()
    {
        var admission = Domain.Admission.Admission.None(Event, TicketType.EventTicket, Ticket);

        var ex = Assert.Throws<DomainException>(() => admission.CheckOut("Tor", Now));

        Assert.Equal(AdmissionEvaluator.NotCheckedIn, ex.Message);
    }

    [Fact]
    public void Check_out_marks_the_visit_outside_and_logs_it()
    {
        var admission = Domain.Admission.Admission.None(Event, TicketType.EventTicket, Ticket);
        var visit = admission.CheckIn(1, "Kasse", Now);

        var left = admission.CheckOut("Tor", Now.AddHours(1));

        Assert.Same(visit, left);
        Assert.False(left.IsInside);
        Assert.Equal(0, admission.InsideCount);
        Assert.Equal(2, left.NewLogs.Count);
        Assert.Equal(VisitLogType.CheckOut, left.NewLogs[1].Type);
        Assert.Equal("Tor", left.NewLogs[1].ScannedBy);
    }

    [Theory]
    [InlineData(TicketType.EventTicket)]
    [InlineData(TicketType.SeasonSingle)]
    [InlineData(TicketType.SeasonPass)]
    public void Re_check_in_reuses_the_single_visit_for_non_member_types(TicketType type)
    {
        var visit = AdmissionVisit.FromPersistence(7, false, Now, null, null,
            [new VisitLog(1, VisitLogType.CheckIn, Now, "Kasse"), new VisitLog(2, VisitLogType.CheckOut, Now.AddHours(1), "Tor")]);
        var admission = Domain.Admission.Admission.FromPersistence(Event, type, Ticket, [visit]);

        var again = admission.CheckIn(1, "Kasse", Now.AddHours(2));

        Assert.Same(visit, again);
        Assert.Single(admission.Visits);
        Assert.True(again.IsInside);
        Assert.True(again.Changed);
        Assert.False(again.IsNew);
        var log = Assert.Single(again.NewLogs);
        Assert.Equal(VisitLogType.CheckIn, log.Type);
        Assert.Equal(3, again.Logs.Count);
    }

    [Fact]
    public void Member_cards_get_a_new_visit_per_admission_and_release_the_latest()
    {
        var admission = Domain.Admission.Admission.None(Event, TicketType.MemberCard, Ticket);

        var first = admission.CheckIn(3, "A", Now);
        var second = admission.CheckIn(3, "B", Now.AddMinutes(5));
        var third = admission.CheckIn(3, "C", Now.AddMinutes(10));

        Assert.True(admission.MultiAdmission);
        Assert.Equal(3, admission.Visits.Count);
        Assert.Equal(3, admission.InsideCount);
        Assert.NotSame(first, second);
        Assert.NotSame(second, third);

        var released = admission.CheckOut("Tor", Now.AddMinutes(20));

        Assert.Same(third, released);
        Assert.False(third.IsInside);
        Assert.True(first.IsInside);
        Assert.True(second.IsInside);
        Assert.Equal(2, admission.InsideCount);
    }

    [Fact]
    public void Check_ins_and_last_check_in_are_ordered_by_time_across_visits()
    {
        var older = AdmissionVisit.FromPersistence(2, true, Now, null, null,
            [new VisitLog(5, VisitLogType.CheckIn, Now.AddMinutes(30), "Later")]);
        var newer = AdmissionVisit.FromPersistence(3, true, Now, null, null,
            [new VisitLog(4, VisitLogType.CheckIn, Now.AddMinutes(10), "Earlier"), new VisitLog(6, VisitLogType.CheckOut, Now.AddMinutes(15), "Out")]);
        var admission = Domain.Admission.Admission.FromPersistence(Event, TicketType.MemberCard, Ticket, [newer, older]);

        var checkIns = admission.CheckIns;

        Assert.Equal(2, checkIns.Count);
        Assert.Equal("Earlier", checkIns[0].ScannedBy);
        Assert.Equal("Later", checkIns[1].ScannedBy);
        Assert.All(checkIns, l => Assert.Equal(VisitLogType.CheckIn, l.Type));
        Assert.Equal("Later", admission.LastCheckIn?.ScannedBy);
    }

    [Fact]
    public void Last_check_in_is_null_without_visits() =>
        Assert.Null(Domain.Admission.Admission.None(Event, TicketType.EventTicket, Ticket).LastCheckIn);

    [Fact]
    public void Mark_persisted_sets_the_id_and_clears_new_logs_and_changed()
    {
        var admission = Domain.Admission.Admission.None(Event, TicketType.EventTicket, Ticket);
        var visit = admission.CheckIn(1, null, Now);

        visit.MarkPersisted(42);

        Assert.Equal(42, visit.Id);
        Assert.False(visit.IsNew);
        Assert.False(visit.Changed);
        Assert.Empty(visit.NewLogs);
        Assert.Single(visit.Logs);
    }

    [Fact]
    public void From_persistence_orders_visits_by_id_and_logs_by_time()
    {
        var late = AdmissionVisit.FromPersistence(9, false, Now, null, null, []);
        var early = AdmissionVisit.FromPersistence(4, false, Now, null, null,
            [new VisitLog(2, VisitLogType.CheckOut, Now.AddMinutes(5), null), new VisitLog(1, VisitLogType.CheckIn, Now, null)]);

        var admission = Domain.Admission.Admission.FromPersistence(Event, TicketType.SeasonPass, Ticket, [late, early]);

        Assert.Equal([4L, 9L], admission.Visits.Select(v => v.Id));
        Assert.Equal(VisitLogType.CheckIn, early.Logs[0].Type);
        Assert.Equal(VisitLogType.CheckOut, early.Logs[1].Type);
        Assert.Equal(Event, admission.EventId);
        Assert.Equal(TicketType.SeasonPass, admission.TicketType);
        Assert.Equal(Ticket, admission.TicketUuid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_cap_below_one_counts_as_one(int cap)
    {
        var admission = Domain.Admission.Admission.None(Event, TicketType.MemberCard, Ticket);
        admission.CheckIn(cap, null, Now);

        var ex = Assert.Throws<DomainException>(() => admission.CheckIn(cap, null, Now.AddMinutes(1)));

        Assert.Equal(AdmissionEvaluator.AlreadyCheckedIn, ex.Message);
        Assert.Equal(1, admission.InsideCount);
    }
}
