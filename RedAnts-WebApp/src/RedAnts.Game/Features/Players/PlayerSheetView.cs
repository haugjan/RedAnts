using RedAnts.Game.Features.Shared;

namespace RedAnts.Game.Features.Players;

public sealed record PointsOriginRow(string Label, int Points);

public sealed record PlayerSheetView(
    PlayerCardView Card,
    int? BirthYear,
    string? Height,
    string? Licence,
    int RawTotal,
    int BestPlayer,
    int PenaltyMinutes,
    decimal GoalsAgainstPerGame,
    int PeakPoints,
    int PeakGame,
    decimal RecentAverage,
    string Season,
    IReadOnlyList<int> Form,
    IReadOnlyList<PointsOriginRow> PointsFrom);
