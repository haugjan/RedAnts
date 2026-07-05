using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Domain.Tests.Sales;

public class EventVisitTests
{
    [Fact]
    public void CreateForTicket_StartsInside_WithTicketUuid()
    {
        var uuid = Guid.NewGuid();
        var visit = EventVisit.CreateForTicket(eventId: 5, TicketType.EventTicket, uuid);

        Assert.True(visit.IsInside);
        Assert.Equal(uuid, visit.TicketUuid);
        Assert.Equal(TicketType.EventTicket, visit.TicketType);
    }

    [Fact]
    public void CreateForTicket_RejectsFreeEntryType()
    {
        Assert.Throws<DomainException>(() =>
            EventVisit.CreateForTicket(5, TicketType.FreeEntry, Guid.NewGuid()));
    }

    [Fact]
    public void CreateForTicket_RejectsInvalidEvent()
    {
        Assert.Throws<DomainException>(() =>
            EventVisit.CreateForTicket(0, TicketType.EventTicket, Guid.NewGuid()));
    }

    [Fact]
    public void CreateForFreeEntry_StartsInside_WithoutTicket()
    {
        var visit = EventVisit.CreateForFreeEntry(eventId: 5);

        Assert.True(visit.IsInside);
        Assert.Null(visit.TicketUuid);
        Assert.Equal(TicketType.FreeEntry, visit.TicketType);
    }

    [Fact]
    public void SetInside_TogglesState()
    {
        var visit = EventVisit.CreateForFreeEntry(5);

        visit.SetInside(false);
        Assert.False(visit.IsInside);

        visit.SetInside(true);
        Assert.True(visit.IsInside);
    }
}
