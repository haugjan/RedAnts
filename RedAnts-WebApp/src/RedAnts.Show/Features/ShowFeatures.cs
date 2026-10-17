using RedAnts.Show.Features.Admin;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Features.Remote;
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
        typeof(OpenDJSound.Handler),
    ];

    public static IServiceCollection AddShowFeatures(this IServiceCollection services)
    {
        foreach (var handler in Handlers)
            services.AddScoped(handler);
        return services;
    }
}
