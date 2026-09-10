using Microsoft.Extensions.Configuration;
using NPoco;
using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Orders.Infrastructure;
using RedAnts.Ticketing.Features.Tickets.Infrastructure;
using Xunit;
using PaymentMethod = RedAnts.Ticketing.Domain.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Tests.Database;

[Collection(AgentDatabaseCollection.Name)]
public class OrderRepositoryDatabaseTests : IAsyncLifetime
{
    private readonly AgentDatabaseFixture _agent = new();

    public Task InitializeAsync() => _agent.InitializeAsync();

    public Task DisposeAsync() => _agent.DisposeAsync();

    private OrderRepository Orders => new(_agent.Scopes, new ConfigurationBuilder().Build());

    private static BillingAddress Address() => BillingAddress.Create(
        BuyerType.Private, "Anna", "Muster", null, "Bahnhofstrasse 1", "c/o Red Ants",
        "8400", "Winterthur", "Schweiz", "anna@example.ch", "079 000 00 00");

    [DatabaseFact]
    public async Task An_order_round_trips_through_the_database()
    {
        var number = await Orders.NextOrderNumberAsync();
        var saved = await Orders.SaveAsync(Order.Create(number, Address(), Money.Of(120m), 0.081m, PaymentMethod.Twint, "CHE-123.456.789"));

        Assert.True(saved.Id > 0);

        var loaded = await Orders.GetByIdAsync(saved.Id);

        Assert.NotNull(loaded);
        Assert.Equal(number, loaded.OrderNumber);
        Assert.Equal(120m, loaded.TotalGross.Amount);
        Assert.Equal(saved.VatAmount, loaded.VatAmount);
        Assert.Equal(saved.SubtotalNet, loaded.SubtotalNet);
        Assert.Equal(OrderStatus.Draft, loaded.Status);
        Assert.Equal(PaymentMethod.Twint, loaded.PaymentMethod);
        Assert.Equal("CHE-123.456.789", loaded.SellerUid);
        Assert.Equal("Winterthur", loaded.BillingAddress.City);
        Assert.Equal("c/o Red Ants", loaded.BillingAddress.AddressLine2);
        Assert.Equal("anna@example.ch", loaded.BillingAddress.Email.Value);
        Assert.Equal(saved.CreatedAt, loaded.CreatedAt);

        Assert.Equal(saved.Id, (await Orders.GetByNumberAsync(number))?.Id);
        Assert.Null(await Orders.GetByIdAsync(0));
    }

    [DatabaseFact]
    public async Task Items_and_refunds_are_read_back_with_the_order()
    {
        var order = await Orders.SaveAsync(Order.Create(
            await Orders.NextOrderNumberAsync(), Address(), Money.Of(60m), 0.081m, PaymentMethod.Twint, null));

        await new OrderItemRepository(_agent.Scopes).SaveAsync(order.Id,
        [
            OrderItem.Create(order.Id, OrderItemKind.EventTicket, _agent.UnusedId, TicketCategory.Adult, "Sitzplatz", 2, Money.Of(20m)),
            OrderItem.Create(order.Id, OrderItemKind.SeasonSingle, _agent.UnusedId, TicketCategory.Youth, "Flexticket", 1, Money.Of(20m))
        ]);

        Assert.True(await Orders.TryMarkPaidAsync(order.Id));

        var refund = await new OrderRefundRepository(_agent.Scopes)
            .CreateAsync(order.Id, Money.Of(20m), RefundMethod.Bank, RefundStatus.Confirmed, "Beleg 1", "Kulanz", "Tester");

        var detail = await new OrderDetailReader(_agent.Scopes).GetAsync(order.Id);

        Assert.NotNull(detail);
        Assert.Equal(60m, detail.TotalGross);
        Assert.Equal(20m, detail.RefundedConfirmed);
        Assert.Equal(40m, detail.Remaining);
        Assert.Equal(2, detail.Items.Count);
        Assert.Equal(40m, detail.Items[0].LineTotal);
        Assert.Equal(refund.RefundNumber, Assert.Single(detail.Refunds).RefundNumber);
        Assert.Equal(OrderStatus.PartiallyRefunded, (await Orders.GetByIdAsync(order.Id))!.Status);
    }

    [DatabaseFact]
    public async Task Only_a_draft_can_be_marked_paid_or_cancelled()
    {
        var order = await Orders.SaveAsync(Order.Create(
            await Orders.NextOrderNumberAsync(), Address(), Money.Of(30m), 0.081m, PaymentMethod.Cash, null));

        Assert.True(await Orders.TryMarkPaidAsync(order.Id));
        Assert.False(await Orders.TryMarkPaidAsync(order.Id));
        Assert.False(await Orders.TryCancelDraftAsync(order.Id));

        var paid = await Orders.GetByIdAsync(order.Id);

        Assert.Equal(OrderStatus.Paid, paid!.Status);
        Assert.NotNull(paid.PaidAt);
    }

    [DatabaseFact]
    public async Task The_billing_address_is_copied_into_the_tickets_of_the_order()
    {
        var order = await Orders.SaveAsync(Order.Create(
            await Orders.NextOrderNumberAsync(), Address(), Money.Of(20m), 0.081m, PaymentMethod.Twint, null));
        var ticket = await new EventTicketRepository(_agent.Scopes)
            .SaveAsync(EventTicket.Create(_agent.UnusedId, TicketCategory.Adult, 20m, order.Id));

        await Orders.CopyBillingToTicketsAsync(order.Id);

        var email = await _agent.Database.ExecuteScalarAsync<string>(
            "SELECT Email FROM EventTickets WHERE Uuid = @0", ticket.Uuid.ToString());

        Assert.Equal("anna@example.ch", email);
    }
}
