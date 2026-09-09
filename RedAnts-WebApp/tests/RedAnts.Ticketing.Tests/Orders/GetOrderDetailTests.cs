using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Orders;
using Xunit;

namespace RedAnts.Ticketing.Tests.Orders;

public class GetOrderDetailTests
{
    private readonly StubOrderDetailReader _reader = new();

    private GetOrderDetail.Handler Handler => new(_reader);

    [Fact]
    public async Task Returns_the_detail_of_the_order()
    {
        var detail = new OrderDetail(7, 100m, 30m, 70m,
            [new OrderItemRow(OrderItemKind.EventTicket, "Spiel", 2, 20m, 40m)],
            [new OrderRefundRow("R-1", SwissTime.Timestamp, 30m, RefundMethod.Cash, RefundStatus.Confirmed, "admin", null)],
            [new OrderLogRow(OrderStatus.Paid, "Online-Kauf", SwissTime.Timestamp, null)]);
        _reader.Details[7] = detail;

        var result = await Handler.HandleAsync(new GetOrderDetail.Query(7));

        Assert.Same(detail, result);
    }

    [Fact]
    public async Task An_unknown_order_is_rejected() =>
        await Assert.ThrowsAsync<DomainException>(() => Handler.HandleAsync(new GetOrderDetail.Query(99)));
}
