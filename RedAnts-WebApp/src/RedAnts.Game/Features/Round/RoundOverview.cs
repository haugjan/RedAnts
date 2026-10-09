using RedAnts.Game.Features.Shared;

namespace RedAnts.Game.Features.Round;

public sealed record RoundHighlight(
    string Kind,
    string Value,
    string Caption,
    PlayerCardView Player);

public sealed record RoundOverview(
    string Label,
    string Season,
    string AsOf,
    int PlayerCount,
    RoundHighlight? Lead,
    IReadOnlyList<RoundHighlight> More)
{
    public bool HasPlayers => PlayerCount > 0;
}
