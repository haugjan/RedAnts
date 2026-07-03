using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

/// <summary>Registers the QR ticket generation slice (S3): the signed-token service, the QR image
/// renderer and the read-only issued-ticket lookup. Standalone composer so it does not touch
/// <c>TicketingComposer</c> (owned by other sessions). Auto-discovered via AddComposers().</summary>
public sealed class TicketTokensComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<ITicketTokens, TicketTokenService>();
        builder.Services.AddSingleton<IQrCodeRenderer, QrCodeRenderer>();
        builder.Services.AddScoped<IIssuedTicketReader, IssuedTicketReader>();
    }
}
