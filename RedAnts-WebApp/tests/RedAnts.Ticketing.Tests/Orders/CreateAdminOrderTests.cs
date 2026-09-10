using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Tests.Checkout;
using Xunit;
using PaymentMethod = RedAnts.Ticketing.Domain.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Tests.Orders;

public class CreateAdminOrderTests
{
    private readonly InMemoryOrderRepository _orders = new();
    private readonly RecordingOrderItems _items = new();
    private readonly RecordingOrderLog _log = new();
    private readonly RecordingUnitOfWork _unitOfWork = new();

    private CreateAdminOrder.Handler Handler => new(_orders, _items, _log, _unitOfWork);

    [Fact]
    public async Task Creates_a_paid_manual_order_with_items_and_two_log_entries()
    {
        var buyer = Buyer.Create(BuyerType.Company, null, null, "UHC Beispiel");
        var lines = new List<AdminOrderLine>
        {
            new(OrderItemKind.EventTicket, 42, TicketCategory.Adult, "Spiel · Erwachsen", 2, Money.Of(20m)),
            new(OrderItemKind.AddOn, 3, default, "Parkplatz", 1, Money.Of(5.555m), Guid.NewGuid())
        };

        var order = await Handler.HandleAsync(new CreateAdminOrder.Command(buyer, "kasse@redants.ch", lines, "kassier", PaymentSource.Cash));

        Assert.True(order.Id > 0);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(PaymentMethod.Manual, order.PaymentMethod);
        Assert.Equal(PaymentSource.Cash, order.PaymentSource);
        Assert.Equal(45.56m, order.TotalGross.Amount);
        Assert.Equal("UHC Beispiel", order.BillingAddress.Company);
        Assert.Equal("kasse@redants.ch", order.BillingAddress.Email.Value);
        Assert.Equal("Schweiz", order.BillingAddress.Country);

        var items = _items.Saved[order.Id];
        Assert.Equal(2, items.Count);
        Assert.Equal(lines[1].ArticleGuid, items[1].ArticleGuid);
        Assert.Equal(40m, items[0].LineTotal.Amount);

        Assert.Equal(
            [(order.Id, OrderStatus.Draft, "kassier", "Im Backoffice erstellt"), (order.Id, OrderStatus.Paid, "kassier", "Backoffice")],
            _log.Entries);
    }

    [Fact]
    public async Task A_private_buyer_keeps_first_and_last_name()
    {
        var buyer = Buyer.Create(BuyerType.Private, "Max", "Muster", null);

        var order = await Handler.HandleAsync(new CreateAdminOrder.Command(buyer, null, [], "kassier", PaymentSource.Sponsoring));

        Assert.Equal("Max", order.BillingAddress.FirstName);
        Assert.Equal("Muster", order.BillingAddress.LastName);
        Assert.Equal("", order.BillingAddress.Email.Value);
        Assert.Equal(0m, order.TotalGross.Amount);
        Assert.Empty(_items.Saved[order.Id]);
    }

    [Fact]
    public async Task Order_items_and_log_are_written_in_one_unit_of_work()
    {
        var order = await Handler.HandleAsync(Command());

        Assert.Equal(1, _unitOfWork.Committed);
        Assert.Equal(0, _unitOfWork.RolledBack);
        Assert.True(_items.Saved.ContainsKey(order.Id));
        Assert.Equal(2, _log.Entries.Count);
    }

    [Fact]
    public async Task A_failing_log_write_rolls_the_whole_order_back()
    {
        _log.Throws = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler.HandleAsync(Command()));

        Assert.Equal(1, _unitOfWork.RolledBack);
        Assert.Equal(0, _unitOfWork.Committed);
        Assert.Empty(_log.Entries);
    }

    private static CreateAdminOrder.Command Command() =>
        new(Buyer.Create(BuyerType.Private, "Anna", "Muster", null), "anna@example.ch",
            [new AdminOrderLine(OrderItemKind.EventTicket, 42, TicketCategory.Adult, "Spiel · Erwachsen", 1, Money.Of(20m))],
            "kassier", PaymentSource.Cash);
}
