using Microsoft.Extensions.Time.Testing;
using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;
using Xunit;
using PaymentMethod = RedAnts.Ticketing.Domain.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Tests.Sales;

public class InjectedTimeTests
{
    private static readonly DateTimeOffset Instant = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SwissInstant = new(2026, 7, 15, 14, 0, 0, TimeSpan.FromHours(2));

    private static FakeTimeProvider Time() => new(Instant);

    private static BillingAddress Address() => BillingAddress.Create(
        BuyerType.Private, "Anna", "Muster", null, "Bahnhofstrasse 1", null,
        "8400", "Winterthur", "Schweiz", "anna@example.ch", null);

    [Fact]
    public void An_order_is_created_and_paid_on_the_injected_clock()
    {
        var time = Time();
        var order = Order.Create("ORD-1", Address(), Money.Of(100m), 0.081m, PaymentMethod.Twint, null, time: time);

        Assert.Equal(SwissInstant, order.CreatedAt);

        time.Advance(TimeSpan.FromHours(1));
        order.MarkPaid(time);

        Assert.Equal(SwissInstant.AddHours(1), order.PaidAt);
    }

    [Fact]
    public void Tickets_passes_and_cards_are_stamped_with_the_injected_clock()
    {
        var time = Time();

        Assert.Equal(SwissInstant, EventTicket.Create(11, TicketCategory.Adult, 20m, null, time: time).CreatedAt);
        Assert.Equal(SwissInstant, SeasonSingleTicket.Create(12, TicketCategory.Adult, 15m, null, time: time).CreatedAt);
        Assert.Equal(SwissInstant, SeasonPass.Create(12, null, 200m, null, time: time).CreatedAt);
        Assert.Equal(SwissInstant, MemberCard.Create(12, MemberCategory.RedAnts, "Anna", "Muster", null, time: time).CreatedAt);
        Assert.Equal(SwissInstant, FlexTicketBundle.Create(12, TicketCategory.Adult, "Bundle", time: time).CreatedAt);
        Assert.Equal(SwissInstant, EventTicketBundle.Create(11, TicketCategory.Adult, "Bundle", time: time).CreatedAt);
        Assert.Equal(SwissInstant, EventVisit.CreateForTicket(11, TicketType.EventTicket, Guid.NewGuid(), time).CreatedAt);
    }

    [Fact]
    public void The_default_clock_stays_the_system_clock()
    {
        var order = Order.Create("ORD-2", Address(), Money.Of(100m), 0.081m, PaymentMethod.Twint, null);

        Assert.InRange((order.CreatedAt - SwissTime.Timestamp).Duration().TotalSeconds, 0, 5);
    }

    [Fact]
    public void The_expiry_window_is_read_from_the_injected_clock()
    {
        var command = ExpireDraftOrders.Command.Due(Time());

        Assert.Equal(SwissInstant - ExpireDraftOrders.LookBack, command.CreatedAfter);
        Assert.Equal(SwissInstant - ExpireDraftOrders.MaxDraftAge, command.CreatedBefore);
    }
}
