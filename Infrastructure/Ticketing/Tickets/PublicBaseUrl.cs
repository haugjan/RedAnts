using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class PublicBaseUrl(IConfiguration config, IHttpContextAccessor httpContextAccessor) : IPublicBaseUrl
{
    public string Resolve()
    {
        var configured = config["Tickets:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');

        var request = httpContextAccessor.HttpContext?.Request;
        return request is not null ? $"{request.Scheme}://{request.Host}" : "";
    }
}

public sealed class PublicBaseUrlComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IPublicBaseUrl, PublicBaseUrl>();
    }
}
