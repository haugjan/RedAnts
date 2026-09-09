using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.Tickets.Infrastructure;

public sealed class TicketsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IEventTicketRepository, EventTicketRepository>();
        builder.Services.AddScoped<IEventTicketListReader, EventTicketListReader>();
        builder.Services.AddScoped<IPrintItemsReader, PrintItemsReader>();
    }
}
