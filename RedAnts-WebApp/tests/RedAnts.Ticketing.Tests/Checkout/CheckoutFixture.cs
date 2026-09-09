using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Checkout;

internal sealed class CheckoutFixture
{
    public const int EventId = 10;
    public const int SeasonId = 3;
    public const int AdultTier = 1;
    public const int PassTier = 7;

    public InMemoryOrderRepository Orders { get; } = new();
    public RecordingOrderLog OrderLog { get; } = new();
    public StubConversionRules ConversionRules { get; } = new();
    public StubAdmission Admission { get; } = new();
    public StubSeasonAddOns SeasonAddOns { get; } = new();
    public StubPayrexx Payrexx { get; } = new();
    public InMemoryEventPrices EventPrices { get; } = new();
    public InMemorySeasonPrices SeasonPrices { get; } = new();
    public InMemoryEventTicketRepository Tickets { get; } = new();
    public InMemorySeasonPasses Passes { get; } = new();
    public StubConvertibleCards ConvertibleCards { get; } = new();
    public RecordingOrderAddOns OrderAddOns { get; } = new();
    public RecordingAddOnNotifier AddOnNotifier { get; } = new();
    public RecordingOrderMailer Mailer { get; } = new();
    public RecordingOrderItems OrderItems { get; } = new();
    public RecordingNewsletterSignupRepository Newsletter { get; } = new();

    public CheckoutFixture()
    {
        EventPrices.Seed(EventPrice.FromPersistence(1, EventId, 100, 120,
            [CategoryPrice.FromPersistence(TicketCategory.Adult, 20m, 10, null, AdultTier)]));
        SeasonPrices.Seed(SeasonPrice.FromPersistence(1, SeasonId, 50,
            [SeasonCategoryPrice.FromPersistence(TicketCategory.Adult, 300m, true, 5, 20m, true, null, null, null, null, PassTier)]));
    }

    public CapacityReservation Reservation => new(EventPrices, SeasonPrices, NullLogger<CapacityReservation>.Instance);

    public OrderFulfillment Fulfillment => new(Orders, OrderLog, Tickets, Passes, ConvertibleCards, OrderAddOns, AddOnNotifier, SeasonAddOns,
        Mailer, new StubPublicBaseUrl(), OrderItems, Newsletter, new EmptyIssuedTickets(), Reservation, NullLogger<OrderFulfillment>.Instance);

    public PlaceOrder.Handler PlaceOrder => new(Orders, OrderLog, ConversionRules, Admission, SeasonAddOns, Payrexx, new StubPublicBaseUrl(),
        new StubOrderTokens(), Reservation, Fulfillment, NullLogger<PlaceOrder.Handler>.Instance);

    public ConfirmPayment.Handler ConfirmPayment => new(Orders, Payrexx, Fulfillment, Reservation, OrderLog, NullLogger<ConfirmPayment.Handler>.Instance);

    public CancelDraftOrder.Handler CancelDraftOrder => new(Orders, OrderLog, Reservation);

    public ExpireDraftOrders.Handler ExpireDraftOrders => new(Orders, new InMemoryDraftOrdersReader(Orders), OrderLog, Reservation, Payrexx, Fulfillment, NullLogger<ExpireDraftOrders.Handler>.Instance);

    public static BillingAddress Billing(string? phone = null) => BillingAddress.Create(
        BuyerType.Private, "Anna", "Muster", null, "Bahnhofstrasse 1", null, "8400", "Winterthur", "Schweiz", "anna@example.ch", phone);

    public static Cart CartWithTickets(int quantity = 2)
    {
        var cart = Cart.Empty();
        cart.AddEventTickets(EventId, "Red Ants vs. Gegner", AdultTier, "Erwachsen", "Erw", 20m, quantity);
        return cart;
    }

    public static Cart CartWithPass(IReadOnlyList<CartAddOn>? addOns = null)
    {
        var cart = Cart.Empty();
        cart.AddSeasonPasses(SeasonId, "Saison 2026/27", PassTier, "Erwachsen", "Erw", 300m, 1, addOns ?? []);
        return cart;
    }

    public async Task<Order> PlacedDraftAsync(Cart? cart = null)
    {
        Payrexx.Enabled = true;
        var result = await PlaceOrder.HandleAsync(new PlaceOrder.Command(cart ?? CartWithTickets(), Billing(), false, CheckoutSource.Checkout));
        var required = Assert.IsType<PlaceOrder.Result.PaymentRequired>(result);
        return Orders.Stored.Single(o => o.Id == required.OrderId);
    }

    public int EventReserved => EventPrices.Stored(EventId).Reserved;

    public int TierReserved => EventPrices.Stored(EventId).Categories.Single(c => c.TierId == AdultTier).Reserved;

    public int PassReserved => SeasonPrices.Stored(SeasonId).Reserved;

    public void HallFull() => Admission.Occupancies[EventId] = new Occupancy(120, 120);
}
