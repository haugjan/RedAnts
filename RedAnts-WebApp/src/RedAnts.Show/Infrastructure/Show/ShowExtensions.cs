namespace RedAnts.Infrastructure.Show;

public static class ShowExtensions
{
    public static IServiceCollection AddShow(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ShowStorageOptions>(config.GetSection(ShowStorageOptions.SectionName));
        return services;
    }

    public static WebApplication UseShow(this WebApplication app) => app;
}
