using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Games.Infrastructure;
using Xunit;

namespace RedAnts.Show.Tests;

public class ClubTeamsTests
{
    private const string ClubTeamsJson = """
        { "type": "dropdown", "entries": [
          { "text": "Junioren E I", "set_in_context": { "team_id": 430428 } },
          { "text": "Damen GF L-UPL", "set_in_context": { "team_id": 428665 } },
          { "text": "Damen GF 1. Liga II", "set_in_context": { "team_id": 418466 } },
          { "text": "Juniorinnen U21 A", "set_in_context": { "team_id": 428661 } },
          { "text": "Juniorinnen U17 A", "set_in_context": { "team_id": 431323 } },
          { "text": "Juniorinnen U17 B II", "set_in_context": { "team_id": 432023 } },
          { "text": "Juniorinnen C I", "set_in_context": { "team_id": 428898 } }
        ] }
        """;

    [Fact]
    public void The_club_list_gives_the_four_show_teams_with_their_league()
    {
        var teams = ClubTeams.Pick(SwissUnihockeyJson.ParseTeams(ClubTeamsJson));

        Assert.Equal(
            [(428665, "L-UPL"), (418466, "1. Liga"), (428661, "U21"), (431323, "U17")],
            teams.Select(t => (t.Id, t.League)).ToList());
    }

    [Theory]
    [InlineData("heute", "19:00", "2026-10-31T19:00")]
    [InlineData("Gestern", "14:30", "2026-10-30T14:30")]
    [InlineData("morgen", "", "2026-11-01T00:00")]
    [InlineData("21.11.2026", "16:00", "2026-11-21T16:00")]
    public void Relative_days_from_swissunihockey_become_dates(string date, string time, string expected)
    {
        var parsed = SwissUnihockeyJson.ParseDate(date, time, new DateOnly(2026, 10, 31));

        Assert.Equal(DateTime.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), parsed);
    }

    [Fact]
    public void Only_games_with_the_red_ants_at_home_count()
    {
        var home = new UpcomingGame("1", null, "Red Ants Winterthur", "UH Berner Oberland", "WIN4", "");
        var away = new UpcomingGame("2", null, "Zug United", "Red Ants Winterthur", "Zug", "");

        Assert.True(ClubTeams.IsHome(home));
        Assert.False(ClubTeams.IsHome(away));
    }
}
