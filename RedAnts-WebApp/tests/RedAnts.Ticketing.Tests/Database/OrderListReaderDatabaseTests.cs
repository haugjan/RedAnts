using Microsoft.Extensions.Configuration;
using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.FlexTickets.Infrastructure;
using RedAnts.Ticketing.Features.Orders.Infrastructure;
using RedAnts.Ticketing.Features.Tickets.Infrastructure;
using Xunit;
using PaymentMethod = RedAnts.Ticketing.Domain.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Tests.Database;

[Collection(AgentDatabaseCollection.Name)]
public class OrderListReaderDatabaseTests : IAsyncLifetime
{
    private readonly AgentDatabaseFixture _agent = new();

    public Task InitializeAsync() => _agent.InitializeAsync();

    public Task DisposeAsync() => _agent.DisposeAsync();

    private OrderRepository Orders => new(_agent.Scopes, new ConfigurationBuilder().Build());

    private static BillingAddress Address(string lastName) => BillingAddress.Create(
        BuyerType.Private, "Anna", lastName, null, "Bahnhofstrasse 1", null,
        "8400", "Winterthur", "Schweiz", "anna@example.ch", null);

    private async Task<Order> OrderAsync(string lastName, decimal total) => await Orders.SaveAsync(Order.Create(
        await Orders.NextOrderNumberAsync(), Address(lastName), total, 0.081m, PaymentMethod.Twint, null));

    [DatabaseFact]
    public async Task Only_the_orders_of_the_season_and_its_events_are_listed()
    {
        var seasonId = _agent.UnusedId;
        var eventId = seasonId + 1;
        var otherSeasonId = seasonId + 2;

        var eventOrder = await OrderAsync("Anlass", 40m);
        await new EventTicketRepository(_agent.Scopes)
            .SaveAsync(EventTicket.Create(eventId, TicketCategory.Adult, 20m, eventOrder.Id));
        await new EventTicketRepository(_agent.Scopes)
            .SaveAsync(EventTicket.Create(eventId, TicketCategory.Youth, 20m, eventOrder.Id));

        var flexOrder = await OrderAsync("Flex", 15m);
        await new FlexTicketBundleRepository(_agent.Scopes)
            .CreateAsync(seasonId, TicketCategory.Adult, "Onlineverkauf", 1, orderId: flexOrder.Id);

        var foreignOrder = await OrderAsync("Fremd", 99m);
        await new EventTicketRepository(_agent.Scopes)
            .SaveAsync(EventTicket.Create(otherSeasonId, TicketCategory.Adult, 99m, foreignOrder.Id));

        var rows = await new OrderListReader(_agent.Scopes).GetBySeasonAsync(seasonId, [eventId]);

        var listed = rows.Where(r => r.OrderId == eventOrder.Id || r.OrderId == flexOrder.Id || r.OrderId == foreignOrder.Id).ToList();

        Assert.Equal(2, listed.Count);
        Assert.DoesNotContain(listed, r => r.OrderId == foreignOrder.Id);

        var withEventTickets = listed.Single(r => r.OrderId == eventOrder.Id);

        Assert.Equal(2, withEventTickets.EventTicketCount);
        Assert.Equal(0, withEventTickets.FlexTicketCount);
        Assert.Equal("Anna Anlass", withEventTickets.BuyerName);
        Assert.Equal(OrderStatus.Draft, withEventTickets.Status);
        Assert.Equal(40m, withEventTickets.TotalGross);

        Assert.Equal(1, listed.Single(r => r.OrderId == flexOrder.Id).FlexTicketCount);
    }

    [DatabaseFact]
    public async Task An_order_without_anything_from_the_season_is_left_out()
    {
        var seasonId = _agent.UnusedId;
        var order = await OrderAsync("Leer", 10m);

        var rows = await new OrderListReader(_agent.Scopes).GetBySeasonAsync(seasonId, []);

        Assert.DoesNotContain(rows, r => r.OrderId == order.Id);
    }
}
