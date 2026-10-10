using RedAnts.Game.Features.Shared;

namespace RedAnts.Game.Features.Standings;

public sealed record StandingRow(
    string ManagerToken,
    string Name,
    int Rank,
    int RoundPoints,
    int TotalPoints,
    int Placed,
    int Spent,
    bool IsMine);

public sealed record StandingsView(
    int Round,
    int RoundsPlayed,
    StandingRow? Mine,
    IReadOnlyList<StandingRow> Rows)
{
    public int Managers => Rows.Count;
    public bool HasField => Rows.Count > 1;
}

public sealed record SquadContribution(PlayerCardView Player, int RoundPoints, int TotalPoints);

public sealed record SquadDetailView(
    string Name,
    int Rank,
    int RoundPoints,
    int TotalPoints,
    int Round,
    IReadOnlyList<int> PointsByRound,
    IReadOnlyList<SquadContribution> Contributions);
