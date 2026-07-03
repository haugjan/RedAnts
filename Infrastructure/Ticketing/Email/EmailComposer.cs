using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Email;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing.Email;

/// <summary>Registers the Brevo e-mail sender (S3 slice). Standalone composer so it does not touch
/// other sessions' composers. Auto-discovered via AddComposers().</summary>
public sealed class EmailComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient("brevo");
        builder.Services.AddScoped<IEmailSender, BrevoEmailSender>();
    }
}
