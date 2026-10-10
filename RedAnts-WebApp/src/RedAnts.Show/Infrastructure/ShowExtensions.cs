using Microsoft.AspNetCore.Mvc.Razor;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Games.Infrastructure;

namespace RedAnts.Show.Infrastructure;

public static class ShowExtensions
{
    public static IServiceCollection AddShow(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient(SwissUnihockeyGames.ClientName, client =>
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RedAnts-Show/1.0"));
        services.Configure<RazorViewEngineOptions>(o =>
        {
            if (!o.ViewLocationExpanders.Any(e => e.GetType().Name == nameof(FeatureViewLocationExpander)))
                o.ViewLocationExpanders.Add(new FeatureViewLocationExpander());
        });
        return services;
    }

    public static IEndpointRouteBuilder MapShow(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<AgentHub>(AgentHub.Route);
        return endpoints;
    }
}
