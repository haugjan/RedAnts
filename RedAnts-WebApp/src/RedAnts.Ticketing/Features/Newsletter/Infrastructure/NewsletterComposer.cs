using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.Newsletter.Infrastructure;

public sealed class NewsletterComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<INewsletterSignupRepository, NewsletterSignupRepository>();
        builder.Services.AddScoped<INewsletterSignupListReader, NewsletterSignupListReader>();
    }
}
