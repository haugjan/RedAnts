namespace RedAnts.Show.Features.Games;

public sealed record ClubTeam(int Id, string Name, string League);

public static class ClubTeams
{
    public const int RedAntsClubId = 45205;
    public const string HomeName = "Red Ants";

    private static readonly (string Pattern, string League)[] Shown =
    [
        ("L-UPL", "L-UPL"),
        ("U21 A", "U21"),
        ("U17 A", "U17"),
        ("1. Liga", "1. Liga"),
    ];

    public static IReadOnlyList<ClubTeam> Pick(IEnumerable<(int Id, string Name)> teams) =>
        teams
            .Select(team => (team, league: Shown.FirstOrDefault(shown => team.Name.Contains(shown.Pattern, StringComparison.OrdinalIgnoreCase)).League))
            .Where(match => match.league is not null)
            .Select(match => new ClubTeam(match.team.Id, match.team.Name, match.league))
            .ToList();

    public static bool IsHome(UpcomingGame game) => game.Home.Contains(HomeName, StringComparison.OrdinalIgnoreCase);
}
