using RedAnts.DJ.Features.Admin;
using RedAnts.DJ.Features.Board;
using RedAnts.DJ.Features.Remote;
using RedAnts.DJ.Features.Sounds;

namespace RedAnts.DJ.Features;

public static class DJFeatures
{
    public static IReadOnlyList<Type> Handlers { get; } =
    [
        typeof(GetDJProfiles.Handler),
        typeof(SaveDJProfiles.Handler),
        typeof(ExportDJBoards.Handler),
        typeof(FindUnusedSounds.Handler),
        typeof(DeleteUnusedSounds.Handler),
        typeof(SetDJSetting.Handler),
        typeof(DispatchDJCommand.Handler),
        typeof(SearchSpotify.Handler),
        typeof(ImportSpotifyContext.Handler),
        typeof(UploadDJSound.Handler),
        typeof(GetSpotifySettings.Handler),
        typeof(TestSpotifyCredentials.Handler),
        typeof(LookupSpotifyReference.Handler),
        typeof(StartSpotifyConnect.Handler),
        typeof(CompleteSpotifyConnect.Handler),
        typeof(GetSpotifyAccessToken.Handler),
        typeof(DisconnectSpotifyAccount.Handler),
        typeof(DownloadDJSound.Handler),
        typeof(RestoreDJSound.Handler),
        typeof(ListenForDJCommands.Handler),
        typeof(PublishBoardView.Handler),
        typeof(WithdrawBoardView.Handler),
        typeof(GetBoardView.Handler),
        typeof(GetDJRooms.Handler),
        typeof(RecordDJRoom.Handler),
        typeof(DeleteDJRoom.Handler),
        typeof(OpenDJSound.Handler)
    ];

    public static IServiceCollection AddDJFeatures(this IServiceCollection services)
    {
        foreach (var handler in Handlers)
            services.AddScoped(handler);
        return services;
    }
}
