using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.FlexTickets.Infrastructure;

public sealed class FlexTicketsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IFlexTicketBundleRepository, FlexTicketBundleRepository>();
        builder.Services.AddScoped<IFlexBundleListReader, FlexBundleListReader>();
        builder.Services.AddScoped<IFlexBundleTicketsReader, FlexBundleTicketsReader>();
    }
}
