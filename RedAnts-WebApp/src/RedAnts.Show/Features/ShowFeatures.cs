using RedAnts.Show.Features.Admin;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Features.Setup;
using RedAnts.Show.Features.Sounds;

namespace RedAnts.Show.Features;

public static class ShowFeatures
{
    public static IReadOnlyList<Type> Handlers { get; } =
    [
        typeof(GetHalls.Handler),
        typeof(GetHall.Handler),
        typeof(CreateHall.Handler),
        typeof(SaveHall.Handler),
        typeof(RenewAgentKey.Handler),
        typeof(DeleteHall.Handler),
        typeof(SetActiveGame.Handler),
        typeof(GetGames.Handler),
        typeof(GetGame.Handler),
        typeof(GetGameSchedule.Handler),
        typeof(LoadGameFromSwissUnihockey.Handler),
        typeof(ImportGameConfig.Handler),
        typeof(SaveGame.Handler),
        typeof(RecordSpectators.Handler),
        typeof(RegisterAgent.Handler),
        typeof(UnregisterAgent.Handler),
        typeof(RecordAgentReport.Handler),
        typeof(GetAgentPayload.Handler),
        typeof(GetHallStatus.Handler),
        typeof(GetAgentPresence.Handler),
        typeof(PanicHall.Handler),
        typeof(PressDeckKey.Handler),
        typeof(PlayHallMusic.Handler),
        typeof(GetDeckView.Handler),
        typeof(RecordDeckView.Handler),
        typeof(GetMusic.Handler),
        typeof(GetMusicLibrary.Handler),
        typeof(SaveMusicEntry.Handler),
        typeof(ImportMusicLibrary.Handler),
        typeof(DeleteMusicEntry.Handler),
        typeof(GetShowSetup.Handler),
        typeof(SaveShowSetup.Handler),
        typeof(GetDJProfiles.Handler),
        typeof(SaveDJProfiles.Handler),
        typeof(FindUnusedSounds.Handler),
        typeof(DeleteUnusedSounds.Handler),
        typeof(SetDJSetting.Handler),
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
        typeof(OpenDJSound.Handler),
    ];

    public static IServiceCollection AddShowFeatures(this IServiceCollection services)
    {
        foreach (var handler in Handlers)
            services.AddScoped(handler);
        return services;
    }
}
