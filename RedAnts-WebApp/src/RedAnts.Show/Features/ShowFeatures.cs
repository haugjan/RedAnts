using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Features.Scenes;

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
        typeof(GetScenes.Handler),
        typeof(SaveScenes.Handler),
        typeof(RegisterAgent.Handler),
        typeof(UnregisterAgent.Handler),
        typeof(RecordAgentReport.Handler),
        typeof(GetAgentPayload.Handler),
        typeof(GetHallStatus.Handler),
        typeof(GetAgentPresence.Handler),
        typeof(RunScene.Handler),
        typeof(PanicHall.Handler),
        typeof(PressDeckKey.Handler),
        typeof(GetDeckView.Handler),
        typeof(RecordDeckView.Handler),
        typeof(GetSounds.Handler),
        typeof(SaveSounds.Handler),
        typeof(UploadSound.Handler),
        typeof(SearchSounds.Handler),
    ];

    public static IServiceCollection AddShowFeatures(this IServiceCollection services)
    {
        foreach (var handler in Handlers)
            services.AddScoped(handler);
        return services;
    }
}
