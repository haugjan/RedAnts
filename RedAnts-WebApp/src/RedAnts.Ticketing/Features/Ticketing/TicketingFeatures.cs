using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.AdmissionWorkflow;
using RedAnts.Features.Ticketing.CardWorkflow;
using RedAnts.Features.Ticketing.CatalogWorkflow;
using RedAnts.Features.Ticketing.CheckoutWorkflow;
using RedAnts.Features.Ticketing.OrderWorkflow;

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
        typeof(GetOccupancy.Handler),
        typeof(SetEventSalesStatus.Handler),
        typeof(SetEventAdmissionQuota.Handler),
        typeof(SetEventSalesQuota.Handler),
        typeof(SetEventPricing.Handler),
        typeof(SetEventConversionRules.Handler),
        typeof(SetEventFreeEntryQuotas.Handler),
        typeof(SetSeasonSalesStatus.Handler),
        typeof(SetSeasonPassQuota.Handler),
        typeof(SetSeasonTicketSalesQuota.Handler),
        typeof(SetSeasonPricing.Handler),
        typeof(SetSeasonAddOns.Handler),
        typeof(ChangeOrderStatus.Handler),
        typeof(RefundOrder.Handler),
        typeof(CreateAdminOrder.Handler),
        typeof(CreateMemberCard.Handler),
        typeof(ImportMemberCards.Handler),
        typeof(EditMemberCard.Handler),
        typeof(SetMemberCardStatus.Handler),
        typeof(SetMemberCardCategory.Handler),
        typeof(DeleteMemberCard.Handler),
        typeof(SendMemberCardMail.Handler),
        typeof(EditSeasonPass.Handler),
        typeof(SetSeasonPassHolder.Handler),
        typeof(DeleteSeasonPass.Handler),
        typeof(ImportSeasonPasses.Handler),
        typeof(SendSeasonPassMail.Handler),
        typeof(AddHelperToSeason.Handler),
        typeof(SetHelperActive.Handler),
        typeof(AssignHelperEvents.Handler),
        typeof(RemoveHelperFromSeason.Handler),
        typeof(InviteHelperByMail.Handler),
        typeof(CreateSeasonPass.Handler),
        typeof(EditEventTicket.Handler),
        typeof(SetEventTicketHolder.Handler),
        typeof(DeleteEventTicket.Handler),
        typeof(CreateEventTicketBundle.Handler),
        typeof(ImportEventTickets.Handler),
        typeof(CreateFlexBundle.Handler),
        typeof(CreateEmptyFlexBundle.Handler),
        typeof(AddFlexTickets.Handler),
        typeof(RenameFlexBundle.Handler),
        typeof(DeleteEmptyFlexBundle.Handler),
        typeof(RebookFlexTicket.Handler),
        typeof(ConvertFlexToBoxOffice.Handler),
        typeof(SetFlexTicketStatus.Handler),
        typeof(SetFlexTicketRedeemed.Handler),
        typeof(SetFlexTicketCategory.Handler),
        typeof(SetFlexTicketHolder.Handler),
        typeof(CreateSingleFlexTicket.Handler),
        typeof(DeleteFlexTicket.Handler),
        typeof(ImportFlexTickets.Handler),
        typeof(SendFlexTicketMail.Handler)
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
