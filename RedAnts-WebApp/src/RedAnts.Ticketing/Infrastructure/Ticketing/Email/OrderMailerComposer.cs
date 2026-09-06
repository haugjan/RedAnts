using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Email;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class OrderMailerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderMailer, OrderMailer>();
}
