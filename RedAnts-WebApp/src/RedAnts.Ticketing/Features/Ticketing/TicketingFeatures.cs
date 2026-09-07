using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.AdmissionWorkflow;
using RedAnts.Features.Ticketing.CheckoutWorkflow;

namespace RedAnts.Features.Ticketing;

public static class TicketingFeatures
{
    public static IReadOnlyList<Type> Handlers { get; } =
    [
        typeof(AddEventTicketsToCart.Handler),
        typeof(AddSeasonPassesToCart.Handler),
        typeof(AddConversionToCart.Handler),
        typeof(ChangeCartLineQuantity.Handler),
        typeof(RemoveOrderAddOnFromCart.Handler),
        typeof(ClearCart.Handler),
        typeof(GetQuickBuyCart.Handler),
        typeof(PlaceOrder.Handler),
        typeof(ConfirmPayment.Handler),
        typeof(CancelDraftOrder.Handler),
        typeof(ExpireDraftOrders.Handler),
        typeof(GetCheckoutStatus.Handler),
        typeof(GetOrderConfirmation.Handler),
        typeof(ScanTicket.Handler),
        typeof(ScanCode.Handler),
        typeof(GrantFreeEntry.Handler),
        typeof(RevokeFreeEntry.Handler),
        typeof(GetOccupancy.Handler)
    ];

    public static IReadOnlyList<Type> Steps { get; } =
    [
        typeof(CapacityReservation),
        typeof(OrderFulfillment),
        typeof(TicketScanning)
    ];

    public static IServiceCollection AddTicketingFeatures(this IServiceCollection services)
    {
        foreach (var handler in Handlers)
            services.AddScoped(handler);
        foreach (var step in Steps)
            services.AddScoped(step);
        return services;
    }
}
