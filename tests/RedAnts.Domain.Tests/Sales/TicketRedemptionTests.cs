using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Domain.Tests.Sales;

public class TicketRedemptionTests
{
    [Fact]
    public void EventTicket_Redeem_MarksRedeemed()
    {
        var ticket = EventTicket.Create(eventId: 5, TicketCategory.Adult, 25m, orderId: 1);

        ticket.Redeem();

        Assert.True(ticket.Redeemed);
    }

    [Fact]
    public void EventTicket_Redeem_WhenNotValid_Throws()
    {
        var ticket = EventTicket.FromPersistence(1, Guid.NewGuid(), 5, TicketCategory.Adult, 25m,
            orderId: 1, TicketStatus.Blocked, DateTime.UtcNow, redeemed: false);

        Assert.Throws<DomainException>(() => ticket.Redeem());
    }

    [Fact]
    public void EventTicket_Create_RejectsInvalidEventOrNegativePrice()
    {
        Assert.Throws<DomainException>(() => EventTicket.Create(0, TicketCategory.Adult, 10m, null));
        Assert.Throws<DomainException>(() => EventTicket.Create(1, TicketCategory.Adult, -1m, null));
    }

    [Fact]
    public void SeasonSingleTicket_Redeem_BindsToEvent()
    {
        var ticket = SeasonSingleTicket.Create(seasonId: 3, TicketCategory.Youth, 10m, orderId: 2);

        ticket.Redeem(eventId: 42);

        Assert.True(ticket.Redeemed);
        Assert.Equal(42, ticket.RedeemedEventId);
    }

    [Fact]
    public void SeasonSingleTicket_Redeem_SameEventTwice_IsIdempotent()
    {
        var ticket = SeasonSingleTicket.Create(3, TicketCategory.Youth, 10m, 2);

        ticket.Redeem(42);
        ticket.Redeem(42);

        Assert.Equal(42, ticket.RedeemedEventId);
    }

    [Fact]
    public void SeasonSingleTicket_Redeem_DifferentEvent_Throws()
    {
        var ticket = SeasonSingleTicket.Create(3, TicketCategory.Youth, 10m, 2);
        ticket.Redeem(42);

        Assert.Throws<DomainException>(() => ticket.Redeem(43));
    }

    [Fact]
    public void SeasonSingleTicket_Redeem_RejectsInvalidEventId()
    {
        var ticket = SeasonSingleTicket.Create(3, TicketCategory.Youth, 10m, 2);

        Assert.Throws<DomainException>(() => ticket.Redeem(0));
    }

    [Fact]
    public void SeasonSingleTicket_Redeem_WhenBlocked_Throws()
    {
        var ticket = SeasonSingleTicket.FromPersistence(1, Guid.NewGuid(), 3, TicketCategory.Youth, 10m,
            orderId: 2, TicketStatus.Cancelled, DateTime.UtcNow, redeemedEventId: null, redeemed: false);

        Assert.Throws<DomainException>(() => ticket.Redeem(42));
    }

    [Fact]
    public void SeasonSingleTicket_CreateForBundle_SetsBundleAndNoOrder()
    {
        var ticket = SeasonSingleTicket.CreateForBundle(3, TicketCategory.Child, 0m, bundleId: 7);

        Assert.Equal(7, ticket.BundleId);
        Assert.Null(ticket.OrderId);
    }

    [Fact]
    public void SeasonSingleTicket_CreateForBundle_RejectsInvalidBundle()
    {
        Assert.Throws<DomainException>(() => SeasonSingleTicket.CreateForBundle(3, TicketCategory.Child, 0m, 0));
    }
}
