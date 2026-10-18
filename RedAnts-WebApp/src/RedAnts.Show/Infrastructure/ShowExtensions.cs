using Microsoft.AspNetCore.Mvc.Razor;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Games.Infrastructure;
using RedAnts.Show.Features.Sounds;

namespace RedAnts.Show.Infrastructure;

public static class ShowExtensions
{
    public static IServiceCollection AddShow(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient(SwissUnihockeyGames.ClientName, client =>
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RedAnts-Show/1.0"));
        services.Configure<DJStorageOptions>(config.GetSection(DJStorageOptions.SectionName));
        services.Configure<RazorViewEngineOptions>(o =>
        {
            if (!o.ViewLocationExpanders.Any(e => e.GetType().Name == nameof(FeatureViewLocationExpander)))
                o.ViewLocationExpanders.Add(new FeatureViewLocationExpander());
        });
        return services;
    }

    public static WebApplication UseShow(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (RenamedPath(context.Request.Path) is { } target)
            {
                var permanent = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method);
                context.Response.Redirect(target + context.Request.QueryString, permanent, preserveMethod: !permanent);
                return;
            }
            await next();
        });
        return app;
    }

    private static string? RenamedPath(PathString path)
    {
        if (path.StartsWithSegments("/api/show", out var rest)) return "/api/dj" + rest;
        if (path.Equals("/admin/halle", StringComparison.OrdinalIgnoreCase)) return "/admin/show/hallen";
        if (path.StartsWithSegments("/show", out rest)) return "/dj" + rest;
        return null;
    }

    public static IEndpointRouteBuilder MapShow(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<AgentHub>(AgentHub.Route);
        return endpoints;
    }
}
