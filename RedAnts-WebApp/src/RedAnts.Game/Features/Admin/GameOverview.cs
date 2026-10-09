using RedAnts.Game.Domain;

namespace RedAnts.Game.Features.Admin;

public sealed record AdminPlayerRow(
    int PlayerId,
    string Name,
    PlayerPosition Position,
    string Club,
    int MarketValue,
    decimal RawAverage,
    int Games,
    PlayerStatus Status,
    bool HasPortrait);

public sealed record AdminSquadRow(
    string ManagerToken,
    string Name,
    int Placed,
    int Spent,
    DateTimeOffset UpdatedAt);

public sealed record GameOverview(
    int PlayerCount,
    int WithPortrait,
    int SquadCount,
    string Season,
    string AsOf,
    IReadOnlyList<AdminPlayerRow> Players,
    IReadOnlyList<AdminSquadRow> Squads);
