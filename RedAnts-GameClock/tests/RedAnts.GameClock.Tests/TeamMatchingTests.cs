using RedAnts.GameClock.Teams;
using Xunit;

namespace RedAnts.GameClock.Tests;

public class TeamMatchingTests
{
    [Theory]
    [InlineData("RED", "Red Ants Winterthur")]
    [InlineData("UBO", "Unihockey Berner Oberland")]
    [InlineData("BEO", "Unihockey Berner Oberland")]
    [InlineData("ZUG", "Zug United")]
    [InlineData("PIR", "piranha chur")]
    [InlineData("ALL", "UHC Alligator Malans")]
    [InlineData("KOEN", "Floorball Köniz")]
    [InlineData("WIL", "SV Wiler-Ersigen")]
    [InlineData("LAU", "UHC Laupen ZH")]
    public void MatchesTheAbbreviationsOfTheLeague(string abbreviation, string teamName) =>
        Assert.Equal(100, TeamMatching.Score(abbreviation, teamName));

    [Fact]
    public void PutsTheRightTeamFirst()
    {
        var teams = new[]
        {
            new DirectoryTeam(1, "Zug United", null),
            new DirectoryTeam(2, "Red Ants Winterthur", null),
            new DirectoryTeam(3, "Unihockey Berner Oberland", null),
        };

        Assert.Equal("Red Ants Winterthur", TeamMatching.Rank("RED", teams)[0].Name);
        Assert.Equal("Unihockey Berner Oberland", TeamMatching.Rank("UBO", teams)[0].Name);
        Assert.Equal("Zug United", TeamMatching.Rank("ZUG", teams)[0].Name);
    }

    [Fact]
    public void ScoresNothingForAnEmptyAbbreviation() =>
        Assert.Equal(0, TeamMatching.Score("", "Red Ants Winterthur"));

    [Fact]
    public void ScoresNothingForLettersTheNameDoesNotHave() =>
        Assert.Equal(0, TeamMatching.Score("XYZ", "Red Ants Winterthur"));

    [Fact]
    public void IgnoresCaseAndUmlauts()
    {
        Assert.Equal(TeamMatching.Score("koen", "Floorball Köniz"), TeamMatching.Score("KOEN", "Floorball Koeniz"));
        Assert.Equal("KOENIZ", TeamMatching.Normalize("Köniz"));
    }
}
