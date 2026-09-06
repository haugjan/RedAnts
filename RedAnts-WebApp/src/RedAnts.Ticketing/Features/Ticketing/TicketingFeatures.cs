using Microsoft.Extensions.DependencyInjection;

namespace RedAnts.Features.Ticketing;

public static class TicketingFeatures
{
    public static IReadOnlyList<Type> Handlers { get; } = [];

    public static IServiceCollection AddTicketingFeatures(this IServiceCollection services)
    {
        foreach (var handler in Handlers)
            services.AddScoped(handler);
        return services;
    }
}
