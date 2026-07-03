using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing.Content;
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
    }
}
