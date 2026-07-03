using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing.Content;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Core.Composing;

namespace RedAnts.Infrastructure.Ticketing;

public class TicketingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Schema migration (creates the ticketing tables at boot)
        builder.AddComponent<TicketingMigrationComponent>();

        // Catalog read-adapters (Umbraco Document Types via the published cache)
        builder.Services.AddScoped<ISeasons, UmbracoSeasons>();
        builder.Services.AddScoped<IVenues, UmbracoVenues>();
        builder.Services.AddScoped<IEvents, UmbracoEvents>();

        // Pricing catalog (per-event and per-season price sets) and its resolved read side
        builder.Services.AddScoped<IEventPrices, EventPriceRepository>();
        builder.Services.AddScoped<ISeasonPrices, SeasonPriceRepository>();
        builder.Services.AddScoped<IEventPricing, EventPricingReader>();
        builder.Services.AddScoped<IEventTickets, EventTicketRepository>();
        builder.Services.AddScoped<ISeasonPasses, SeasonPassRepository>();
    }
}
