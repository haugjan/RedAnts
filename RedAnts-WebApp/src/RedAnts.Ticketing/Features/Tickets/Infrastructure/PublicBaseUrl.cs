using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.Tickets.Infrastructure;

public sealed class PublicBaseUrl(IConfiguration config, IHttpContextAccessor httpContextAccessor) : IPublicBaseUrl
{
    public string Resolve()
    {
        var configured = config["Tickets:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');

        var request = httpContextAccessor.HttpContext?.Request;
        return request is not null ? $"{request.Scheme}://{request.Host}" : "";
    }

    public string TicketUrl(string token)
    {
        var shortBase = config["Tickets:ShortBaseUrl"];
        return string.IsNullOrWhiteSpace(shortBase)
            ? $"{Resolve()}/ticket/{token}"
            : $"{shortBase.TrimEnd('/')}/{token}";
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
