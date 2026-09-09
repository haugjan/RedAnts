using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.Checkout.Infrastructure;

public sealed class OrderMailerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderMailer, OrderMailer>();
}
