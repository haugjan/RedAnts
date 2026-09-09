using RedAnts.Features.Show.ShowWorkflow;

namespace RedAnts.Features.Show;

public static class ShowFeatures
{
    public static IReadOnlyList<Type> Handlers { get; } =
    [
        typeof(GetShowProfiles.Handler),
        typeof(SaveShowProfiles.Handler),
        typeof(SetShowSetting.Handler),
        typeof(DispatchShowCommand.Handler),
        typeof(SearchSpotify.Handler),
        typeof(ImportSpotifyContext.Handler),
        typeof(UploadShowSound.Handler)
    ];

    public static IServiceCollection AddShowFeatures(this IServiceCollection services)
    {
        foreach (var handler in Handlers)
            services.AddScoped(handler);
        return services;
    }
}
