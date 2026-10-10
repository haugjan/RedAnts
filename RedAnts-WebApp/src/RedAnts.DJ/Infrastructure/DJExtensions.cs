using Microsoft.AspNetCore.Mvc.Razor;
using RedAnts.DJ.Features.Sounds;

namespace RedAnts.DJ.Infrastructure;

public static class DJExtensions
{
    public static IServiceCollection AddDJ(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<DJStorageOptions>(config.GetSection(DJStorageOptions.SectionName));
        services.Configure<RazorViewEngineOptions>(o =>
        {
            if (!o.ViewLocationExpanders.Any(e => e.GetType().Name == nameof(FeatureViewLocationExpander)))
                o.ViewLocationExpanders.Add(new FeatureViewLocationExpander());
        });
        return services;
    }

    public static WebApplication UseDJ(this WebApplication app) => app;
}
