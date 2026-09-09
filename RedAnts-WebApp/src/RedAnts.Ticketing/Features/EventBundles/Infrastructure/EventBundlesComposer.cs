using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.EventBundles.Infrastructure;

public sealed class EventBundlesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IEventTicketBundleRepository, EventTicketBundleRepository>();
        builder.Services.AddScoped<IEventBundleListReader, EventBundleListReader>();
        builder.Services.AddScoped<IEventBundleTicketsReader, EventBundleTicketsReader>();
    }
}
