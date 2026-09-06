using RedAnts.Features.Ticketing.Cart;

namespace RedAnts.Infrastructure.Ticketing;

public static class TicketingServiceCollectionExtensions
{
    public static IServiceCollection AddTicketing(this IServiceCollection services, IConfiguration config)
    {
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
        services.AddScoped<ICartService, SessionCartService>();

        return services;
    }
}
