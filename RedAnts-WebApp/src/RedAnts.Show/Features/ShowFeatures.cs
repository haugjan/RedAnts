using RedAnts.Show.Features.Admin;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Remote;
using RedAnts.Show.Features.Sounds;

namespace RedAnts.Show.Features;

public static class ShowFeatures
{
    public static IReadOnlyList<Type> Handlers { get; } =
    [
        typeof(GetShowProfiles.Handler),
        typeof(SaveShowProfiles.Handler),
        typeof(SetShowSetting.Handler),
        typeof(DispatchShowCommand.Handler),
        typeof(SearchSpotify.Handler),
        typeof(UploadShowSound.Handler),
        typeof(GetSpotifySettings.Handler),
        typeof(TestSpotifyCredentials.Handler),
        typeof(LookupSpotifyReference.Handler),
        typeof(DownloadShowSound.Handler),
        typeof(RestoreShowSound.Handler),
        typeof(ListenForShowCommands.Handler),
        typeof(OpenShowSound.Handler)
    ];

    public static IServiceCollection AddShowFeatures(this IServiceCollection services)
    {
        foreach (var handler in Handlers)
            services.AddScoped(handler);
        return services;
    }
}
