using Microsoft.AspNetCore.Mvc.Razor;

namespace RedAnts.Game.Infrastructure;

public static class GameExtensions
{
    public static IServiceCollection AddGame(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RazorViewEngineOptions>(o =>
        {
            if (!o.ViewLocationExpanders.Any(e => e.GetType().Name == nameof(GameFeatureViewLocationExpander)))
                o.ViewLocationExpanders.Add(new GameFeatureViewLocationExpander());
        });
        return services;
    }

    public static WebApplication UseGame(this WebApplication app) => app;
}
