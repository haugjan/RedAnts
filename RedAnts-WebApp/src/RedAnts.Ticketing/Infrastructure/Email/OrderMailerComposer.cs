using Microsoft.Extensions.DependencyInjection;
using RedAnts.Ticketing.Features.Email;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Ticketing.Infrastructure.Email;

public sealed class OrderMailerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderMailer, OrderMailer>();
}
