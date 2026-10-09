using RedAnts.Game.Domain;
using RedAnts.Game.Features.Players;
using RedAnts.Game.Features.Settings;

namespace RedAnts.Game.Features.Round;

public static class GetRoundHighlights
{
    public sealed record Query;

    public sealed class Handler(IRoundReader rounds, IGamePlayerRepository players, IGameSettings settings)
    {
        public async Task<RoundOverview> HandleAsync(Query query)
        {
            var highlights = await rounds.GetHighlightsAsync();
            var count = await players.CountAsync();

            return new RoundOverview(
                settings.Get(GameSettingKeys.RoundLabel) ?? GameSettingKeys.DefaultRoundLabel,
                settings.Get(GameSettingKeys.Season) ?? "",
                settings.Get(GameSettingKeys.AsOf) ?? "",
                count,
                highlights.FirstOrDefault(),
                highlights.Skip(1).ToList());
        }
    }
}
