using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing.Content;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Core.Composing;

namespace RedAnts.Infrastructure.Ticketing;

public class TicketingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddComponent<TicketingMigrationComponent>();

        builder.Services.AddScoped<ISeasons, UmbracoSeasons>();
        builder.Services.AddScoped<IVenues, UmbracoVenues>();
        builder.Services.AddScoped<IEvents, UmbracoEvents>();

        builder.Services.AddScoped<IEventPrices, EventPriceRepository>();
        builder.Services.AddScoped<ISeasonPrices, SeasonPriceRepository>();
        builder.Services.AddScoped<IEventPricing, EventPricingReader>();
        builder.Services.AddScoped<ISeasonPassPricing, SeasonPassPricingReader>();
        builder.Services.AddScoped<IEventTickets, EventTicketRepository>();
        builder.Services.AddScoped<ISeasonPasses, SeasonPassRepository>();
        builder.Services.AddScoped<IFlexTicketBundles, FlexTicketBundleRepository>();
        builder.Services.AddScoped<IMemberCards, MemberCardRepository>();
        builder.Services.AddScoped<IOrders, OrderRepository>();
        builder.Services.AddScoped<INewsletterSignups, NewsletterSignupRepository>();
    }
}
