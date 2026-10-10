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

    public static WebApplication UseDJ(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (LegacyShowPath(context.Request.Path) is { } target)
            {
                var permanent = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method);
                context.Response.Redirect(target + context.Request.QueryString, permanent, preserveMethod: !permanent);
                return;
            }
            await next();
        });
        return app;
    }

    private static string? LegacyShowPath(PathString path)
    {
        if (path.StartsWithSegments("/api/show", out var rest)) return "/api/dj" + rest;
        if (path.StartsWithSegments("/admin/show", out rest)) return "/admin/dj" + rest;
        if (path.StartsWithSegments("/show", out rest)) return "/dj" + rest;
        return null;
    }
}
