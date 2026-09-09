using PdfSharp.Fonts;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Catalog.Infrastructure;
using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Features.Checkout.Infrastructure;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.Email.Infrastructure;
using RedAnts.Ticketing.Features.EventBundles;
using RedAnts.Ticketing.Features.EventBundles.Infrastructure;
using RedAnts.Ticketing.Features.FlexTickets;
using RedAnts.Ticketing.Features.FlexTickets.Infrastructure;
using RedAnts.Ticketing.Features.Helpers;
using RedAnts.Ticketing.Features.Helpers.Infrastructure;
using RedAnts.Ticketing.Features.MemberCards;
using RedAnts.Ticketing.Features.MemberCards.Infrastructure;
using RedAnts.Ticketing.Features.Newsletter;
using RedAnts.Ticketing.Features.Newsletter.Infrastructure;
using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Features.Orders.Infrastructure;
using RedAnts.Ticketing.Features.SeasonPasses;
using RedAnts.Ticketing.Features.SeasonPasses.Infrastructure;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Features.Tickets.Infrastructure;
using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Infrastructure;

public class TicketingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddComponent<TicketingMigrationComponent>();

        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new FlexPrintFontResolver();
        builder.Services.AddScoped<ITicketPrinter, TicketPrinting>();
        builder.Services.AddScoped<ITicketPrintSettings, TicketPrintSettingsRepository>();

        builder.Services.AddScoped<ISeasonReader, UmbracoSeasons>();
        builder.Services.AddScoped<IVenueReader, UmbracoVenues>();
        builder.Services.AddScoped<IEventReader, UmbracoEvents>();
        builder.Services.AddScoped<IContentUrls, UmbracoContentUrls>();
        builder.Services.AddScoped<ITicketingHomeReader, TicketingHomeReader>();
        builder.Services.AddScoped<IEventForSaleReader, EventForSaleReader>();
        builder.Services.AddScoped<ISeasonForSaleReader, SeasonForSaleReader>();
        builder.Services.AddScoped<INextEventReader, NextEventReader>();
        builder.Services.AddScoped<IEventsForAdminReader, EventsForAdminReader>();
        builder.Services.AddScoped<ISeasonsForAdminReader, SeasonsForAdminReader>();

        builder.Services.AddScoped<IEventPriceRepository, EventPriceRepository>();
        builder.Services.AddScoped<ISeasonPriceRepository, SeasonPriceRepository>();
        builder.Services.AddScoped<IPriceTierRepository, PriceTierRepository>();
        builder.Services.AddScoped<ITierSalesReader, TierSalesReader>();
        builder.Services.AddScoped<ICapacityUsageReader, CapacityUsageReader>();
        builder.Services.AddScoped<ICheckoutOrderReader, CheckoutOrderReader>();
        builder.Services.AddScoped<IEventPricing, EventPricingReader>();
        builder.Services.AddScoped<ISeasonPassPricing, SeasonPassPricingReader>();
        builder.Services.AddScoped<IEventTickets, EventTicketRepository>();
        builder.Services.AddScoped<ISeasonPasses, SeasonPassRepository>();
        builder.Services.AddScoped<IFlexTicketBundles, FlexTicketBundleRepository>();
        builder.Services.AddScoped<IEventTicketBundles, EventTicketBundleRepository>();
        builder.Services.AddScoped<IMemberCards, MemberCardRepository>();
        builder.Services.AddScoped<IOrders, OrderRepository>();
        builder.Services.AddScoped<INewsletterSignups, NewsletterSignupRepository>();
        builder.Services.AddScoped<ISeasonAddOnRepository, SeasonAddOnRepository>();
        builder.Services.AddScoped<IOrderAddOns, OrderAddOnRepository>();
        builder.Services.AddScoped<IOrderItems, OrderItemRepository>();
        builder.Services.AddScoped<IAddOnNotifier, AddOnNotifier>();
        builder.Services.AddScoped<IHelpers, HelperMemberRepository>();
    }
}
