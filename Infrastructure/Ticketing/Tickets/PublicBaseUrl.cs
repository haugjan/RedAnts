using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class PublicBaseUrl(IConfiguration config) : IPublicBaseUrl
{
    public string Resolve(HttpRequest request)
    {
        var configured = config["Tickets:PublicBaseUrl"];
        return !string.IsNullOrWhiteSpace(configured)
            ? configured.TrimEnd('/')
            : $"{request.Scheme}://{request.Host}";
    }
}

public sealed class PublicBaseUrlComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IPublicBaseUrl, PublicBaseUrl>();
}
