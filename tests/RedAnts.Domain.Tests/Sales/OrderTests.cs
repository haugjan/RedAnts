using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;
using PaymentMethod = RedAnts.Domain.Ticketing.Sales.PaymentMethod;

namespace RedAnts.Domain.Tests.Sales;

public class OrderTests
{
    private static BillingAddress SampleAddress() => BillingAddress.Create(
        BuyerType.Private, "Anna", "Muster", null, "Bahnhofstrasse 1", null,
        "8400", "Winterthur", "Schweiz", "anna@example.ch", null);

    [Fact]
    public void Create_SplitsGrossIntoNetAndVat_ForSwissRate()
    {
        var order = Order.Create("ORD-1", SampleAddress(), totalGross: 100m,
            vatRate: 0.081m, PaymentMethod.Twint, sellerUid: null);

        Assert.Equal(100m, order.TotalGross);
        Assert.Equal(7.49m, order.VatAmount);
        Assert.Equal(92.51m, order.SubtotalNet);
        Assert.Equal(order.TotalGross, order.SubtotalNet + order.VatAmount);
    }

    [Fact]
    public void Create_WithZeroVatRate_LeavesFullAmountAsNet()
    {
        var order = Order.Create("ORD-2", SampleAddress(), totalGross: 50m,
            vatRate: 0m, PaymentMethod.Cash, sellerUid: null);

        Assert.Equal(0m, order.VatAmount);
        Assert.Equal(50m, order.SubtotalNet);
    }

    [Fact]
    public void Create_RoundsGrossToTwoDecimals()
    {
        var order = Order.Create("ORD-3", SampleAddress(), totalGross: 19.999m,
            vatRate: 0m, PaymentMethod.Invoice, sellerUid: null);

        Assert.Equal(20.00m, order.TotalGross);
    }

    [Fact]
    public void Create_DefaultsToChfAndDraftStatus()
    {
        var order = Order.Create("ORD-4", SampleAddress(), 10m, 0m, PaymentMethod.Twint, null);

        Assert.Equal("CHF", order.Currency);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Null(order.PaidAt);
    }

    [Fact]
    public void Create_TrimsOrderNumberAndSellerUid()
    {
        var order = Order.Create("  ORD-5  ", SampleAddress(), 10m, 0m, PaymentMethod.Twint, "  CHE-123  ");

        Assert.Equal("ORD-5", order.OrderNumber);
        Assert.Equal("CHE-123", order.SellerUid);
    }

    [Fact]
    public void Create_BlankSellerUid_BecomesNull()
    {
        var order = Order.Create("ORD-6", SampleAddress(), 10m, 0m, PaymentMethod.Twint, "   ");

        Assert.Null(order.SellerUid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankOrderNumber(string orderNumber)
    {
        Assert.Throws<DomainException>(() =>
            Order.Create(orderNumber, SampleAddress(), 10m, 0m, PaymentMethod.Twint, null));
    }

    [Fact]
    public void Create_RejectsNegativeTotal()
    {
        Assert.Throws<DomainException>(() =>
            Order.Create("ORD-7", SampleAddress(), -1m, 0m, PaymentMethod.Twint, null));
    }

    [Fact]
    public void MarkPaid_SetsStatusAndTimestampOnce()
    {
        var order = Order.Create("ORD-8", SampleAddress(), 10m, 0m, PaymentMethod.Twint, null);

        order.MarkPaid();
        var firstPaidAt = order.PaidAt;
        order.MarkPaid();

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.NotNull(order.PaidAt);
        Assert.Equal(firstPaidAt, order.PaidAt);
    }

    [Fact]
    public void MarkPaid_OnCancelledOrder_Throws()
    {
        var order = Order.Create("ORD-9", SampleAddress(), 10m, 0m, PaymentMethod.Twint, null);
        order.Cancel();

        Assert.Throws<DomainException>(() => order.MarkPaid());
    }

    [Fact]
    public void CancelAndRefund_SetStatus()
    {
        var cancelled = Order.Create("ORD-10", SampleAddress(), 10m, 0m, PaymentMethod.Twint, null);
        cancelled.Cancel();
        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);

        var refunded = Order.Create("ORD-11", SampleAddress(), 10m, 0m, PaymentMethod.Twint, null);
        refunded.Refund();
        Assert.Equal(OrderStatus.Refunded, refunded.Status);
    }
}
