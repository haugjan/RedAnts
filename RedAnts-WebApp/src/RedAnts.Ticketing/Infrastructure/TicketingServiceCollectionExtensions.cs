using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RedAnts.Ticketing.Features;
using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Features.Checkout.Infrastructure;

namespace RedAnts.Ticketing.Infrastructure;

public static class TicketingServiceCollectionExtensions
{
    public static IServiceCollection AddTicketing(this IServiceCollection services, IConfiguration config)
    {
        services.TryAddSingleton(TimeProvider.System);

        var sessionCacheConnectionString = config.GetConnectionString("umbracoDbDSN");
        if (!string.IsNullOrWhiteSpace(sessionCacheConnectionString))
        {
            services.AddDistributedSqlServerCache(options =>
            {
                options.ConnectionString = sessionCacheConnectionString;
                options.SchemaName = SessionCacheSchema.SchemaName;
                options.TableName = SessionCacheSchema.TableName;
            });
        }

        services.AddSession(options =>
        {
            options.Cookie.Name = "RedAnts.Cart";
            options.Cookie.IsEssential = true;
            options.IdleTimeout = TimeSpan.FromDays(7);
        });
        services.AddScoped<ICartRepository, SessionCartRepository>();
        services.Configure<RazorViewEngineOptions>(o =>
        {
            if (!o.ViewLocationExpanders.Any(e => e.GetType().Name == nameof(FeatureViewLocationExpander)))
                o.ViewLocationExpanders.Add(new FeatureViewLocationExpander());
        });
        services.AddTicketingFeatures();

        return services;
    }
}
