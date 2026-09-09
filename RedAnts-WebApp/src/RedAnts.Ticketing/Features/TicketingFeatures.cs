using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Features.EventBundles;
using RedAnts.Ticketing.Features.FlexTickets;
using RedAnts.Ticketing.Features.Helpers;
using RedAnts.Ticketing.Features.MemberCards;
using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Features.SeasonPasses;
using RedAnts.Ticketing.Features.Stats;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features;

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
        typeof(GetFreeEntries.Handler),
        typeof(GetVisits.Handler),
        typeof(GetEventAdmissionReport.Handler),
        typeof(DeleteFreeEntry.Handler),
        typeof(ResolveScannedCode.Handler),
        typeof(GetEventsForScanning.Handler),
        typeof(GetFlexBundles.Handler),
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
        typeof(GetOrdersForAdmin.Handler),
        typeof(GetOrderDetail.Handler),
        typeof(GetOrderAddOnsForAdmin.Handler),
        typeof(SetOrderAddOnDelivered.Handler),
        typeof(GetSeasonsForAdmin.Handler),
        typeof(GetSeasonStats.Handler),
        typeof(GetSalesStats.Handler),
        typeof(GetEventStats.Handler),
        typeof(GetVisitorStats.Handler),
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
        typeof(GetEventTicketsForAdmin.Handler),
        typeof(GetTicketsForExport.Handler),
        typeof(GetTicketPrintSettings.Handler),
        typeof(GetEventTicketMailDefaults.Handler),
        typeof(SendEventTicketMail.Handler),
        typeof(CreateEventTicket.Handler),
        typeof(PrintTickets.Handler),
        typeof(GetEventsForAdmin.Handler),
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
