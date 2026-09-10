using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Tests.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Orders;

public class CanRefundTests
{
    private readonly InMemoryOrderRepository _orders = new();
    private readonly RecordingOrderRefunds _refunds = new();
    private readonly RefundingPayrexx _payrexx = new();

    private CanRefund.Handler Handler => new(_orders, _refunds, _payrexx);

    private async Task<CheckResult.Denied> DeniedAsync(CanRefund.Check check) =>
        Assert.IsType<CheckResult.Denied>(await Handler.HandleAsync(check));

    [Fact]
    public async Task A_paid_order_may_be_refunded()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);

        Assert.True((await Handler.HandleAsync(new CanRefund.Check(order.Id))).IsAllowed);
        Assert.True((await Handler.HandleAsync(new CanRefund.Check(order.Id, Money.Of(40m)))).IsAllowed);
    }

    [Fact]
    public async Task An_unknown_order_is_denied()
    {
        var denied = await DeniedAsync(new CanRefund.Check(404));

        Assert.IsType<RefundDenied.OrderUnknown>(denied.Cause);
        Assert.Equal("Bestellung wurde nicht gefunden.", denied.Cause.Message);
    }

    [Fact]
    public async Task An_unpaid_order_is_denied()
    {
        var order = await OrderFixtures.DraftOrderAsync(_orders);

        var denied = await DeniedAsync(new CanRefund.Check(order.Id));

        Assert.IsType<RefundDenied.NotPaid>(denied.Cause);
        Assert.Equal("Nur bezahlte Bestellungen können zurückerstattet werden.", denied.Cause.Message);
    }

    [Fact]
    public async Task A_fully_refunded_order_is_denied()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);
        await _refunds.CreateAsync(order.Id, Money.Of(100m), RefundMethod.Cash, RefundStatus.Confirmed, null, null, "admin");

        var denied = await DeniedAsync(new CanRefund.Check(order.Id));

        Assert.IsType<RefundDenied.NothingLeft>(denied.Cause);
        Assert.Equal("Diese Bestellung ist vollständig zurückerstattet.", denied.Cause.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task A_non_positive_amount_is_denied(decimal amount)
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);

        var denied = await DeniedAsync(new CanRefund.Check(order.Id, Money.Stored(amount)));

        Assert.IsType<RefundDenied.AmountNotPositive>(denied.Cause);
        Assert.Equal("Betrag muss grösser als 0 sein.", denied.Cause.Message);
    }

    [Fact]
    public async Task An_amount_above_the_open_rest_is_denied()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);
        await _refunds.CreateAsync(order.Id, Money.Of(70m), RefundMethod.Cash, RefundStatus.Confirmed, null, null, "admin");

        var denied = await DeniedAsync(new CanRefund.Check(order.Id, Money.Of(40m)));

        Assert.IsType<RefundDenied.AmountAboveRemaining>(denied.Cause);
        Assert.Contains("30.00", denied.Cause.Message);
    }

    [Fact]
    public async Task A_payrexx_refund_of_an_order_paid_offline_is_denied()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);

        var denied = await DeniedAsync(new CanRefund.Check(order.Id, Money.Of(10m), ViaPayrexx: true));

        Assert.IsType<RefundDenied.NotPaidOnline>(denied.Cause);
    }

    [Fact]
    public async Task A_payrexx_refund_is_denied_while_the_gateway_is_off()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders, gatewayId: "gw-3");
        _payrexx.Enabled = false;

        var denied = await DeniedAsync(new CanRefund.Check(order.Id, Money.Of(10m), ViaPayrexx: true));

        Assert.IsType<RefundDenied.NotPaidOnline>(denied.Cause);
    }

    [Fact]
    public async Task A_payrexx_refund_of_an_online_order_is_allowed()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders, gatewayId: "gw-3");

        Assert.True((await Handler.HandleAsync(new CanRefund.Check(order.Id, Money.Of(10m), ViaPayrexx: true))).IsAllowed);
    }
}
