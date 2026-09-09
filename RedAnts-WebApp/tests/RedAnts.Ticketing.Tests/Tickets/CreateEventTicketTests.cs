using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Tests.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Tickets;

public class CreateEventTicketTests
{
    private static readonly Buyer Anna = Buyer.Create(BuyerType.Private, "Anna", "Muster", null);

    private static readonly CardHolder Holder =
        CardHolder.Create(BuyerType.Private, null, null, "Anna", "Muster", null, "anna@example.ch", null, null, null, null, null, null);

    [Fact]
    public async Task Saves_the_ticket_for_the_order_and_sets_its_holder()
    {
        var tickets = new InMemoryEventTicketRepository();
        var handler = new CreateEventTicket.Handler(tickets);

        var uuid = await handler.HandleAsync(new CreateEventTicket.Command(10, TicketCategory.Youth, 12m, 7, Anna, "admin", "admin@redants.ch", Holder));

        var saved = Assert.Single(tickets.Stored);
        Assert.Equal(uuid, saved.Uuid);
        Assert.Equal((10, TicketCategory.Youth, 12m, 7), (saved.EventId, saved.Category, saved.Price, saved.OrderId));
        Assert.Equal("admin", saved.CreatedByName);
        Assert.Equal((uuid, Holder), Assert.Single(tickets.Holders));
    }

    [Fact]
    public async Task A_negative_price_is_rejected()
    {
        var tickets = new InMemoryEventTicketRepository();

        await Assert.ThrowsAsync<DomainException>(() => new CreateEventTicket.Handler(tickets).HandleAsync(
            new CreateEventTicket.Command(10, TicketCategory.Adult, -1m, 7, Anna, null, null, Holder)));

        Assert.Empty(tickets.Stored);
    }
}
