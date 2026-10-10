using RedAnts.Game.Features.Admin;
using RedAnts.Game.Features.Players;
using RedAnts.Game.Features.Round;
using RedAnts.Game.Features.Settings;
using RedAnts.Game.Features.Standings;
using RedAnts.Game.Features.Squad;

namespace RedAnts.Game.Features;

public static class GameFeatures
{
    public static IReadOnlyList<Type> Handlers { get; } =
    [
        typeof(GetRoundHighlights.Handler),
        typeof(GetMarketPlayers.Handler),
        typeof(GetPlayerSheet.Handler),
        typeof(ImportGamePlayers.Handler),
        typeof(GetMySquad.Handler),
        typeof(PlacePlayerInSquad.Handler),
        typeof(ClearSquadSlot.Handler),
        typeof(RenameSquad.Handler),
        typeof(GetGameSettings.Handler),
        typeof(SetGameSetting.Handler),
        typeof(GetGameOverview.Handler),
        typeof(GetStandings.Handler),
        typeof(GetSquadDetail.Handler),
        typeof(CreateDemoSquads.Handler),
        typeof(DeleteDemoSquads.Handler)
    ];

    public static IServiceCollection AddGameFeatures(this IServiceCollection services)
    {
        foreach (var handler in Handlers)
            services.AddScoped(handler);
        return services;
    }
}
