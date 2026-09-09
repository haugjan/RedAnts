using Microsoft.AspNetCore.Mvc.Razor;
using RedAnts.Show.Features.Sounds;

namespace RedAnts.Show.Infrastructure;

public static class ShowExtensions
{
    public static IServiceCollection AddShow(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ShowStorageOptions>(config.GetSection(ShowStorageOptions.SectionName));
        services.Configure<RazorViewEngineOptions>(o =>
        {
            if (!o.ViewLocationExpanders.Any(e => e.GetType().Name == nameof(FeatureViewLocationExpander)))
                o.ViewLocationExpanders.Add(new FeatureViewLocationExpander());
        });
        return services;
    }

    public static WebApplication UseShow(this WebApplication app) => app;
}
