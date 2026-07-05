using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class TicketTokensComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<ITicketTokens, TicketTokenService>();
        builder.Services.AddSingleton<IQrCodeRenderer, QrCodeRenderer>();
        builder.Services.AddScoped<IIssuedTicketReader, IssuedTicketReader>();
    }
}
