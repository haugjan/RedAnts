using Umbraco.Cms.Core.Composing;
using Umbraco.Extensions;

namespace RedAnts.Ticketing.Features.Tickets.Infrastructure;

public sealed class TicketTokensComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<ITicketTokens, TicketTokenSigner>();
        builder.Services.AddSingleton<IQrCodeRenderer, QrCodeRenderer>();
        builder.Services.AddScoped<IIssuedTicketReader, IssuedTicketReader>();
        builder.Services.AddScoped<ITicketCustomNames, TicketCustomNames>();
        builder.Services.AddScoped<IMyTicketsReader, MyTicketsReader>();
    }
}
