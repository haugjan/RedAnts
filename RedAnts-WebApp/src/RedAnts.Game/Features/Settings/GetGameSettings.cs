using RedAnts.Game.Domain;

namespace RedAnts.Game.Features.Settings;

public sealed record GameSettingsView(int Budget, int Round, string RoundLabel, string Season, string AsOf);

public static class GetGameSettings
{
    public sealed record Query;

    public sealed class Handler(IGameSettings settings)
    {
        public Task<GameSettingsView> HandleAsync(Query query) => Task.FromResult(new GameSettingsView(
            settings.Budget,
            settings.Round,
            settings.Get(GameSettingKeys.RoundLabel) ?? GameSettingKeys.DefaultRoundLabel,
            settings.Get(GameSettingKeys.Season) ?? "",
            settings.Get(GameSettingKeys.AsOf) ?? ""));
    }
}
