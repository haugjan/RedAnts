using RedAnts.Domain;
using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests;

public class GameConfigFileTests
{
    private const string Example = """
        [Game]
        GameNr=897281
        LeagueId=luplw
        Title=NLB
        Date=23.06.2023
        Time=20:00
        Location=Buchholz, Uster
        Referee1=Josef Fässler
        Referee2=Benjamin Schläpfer
        Commentator1=Tom Surbeck
        Commentator2=Raphi Berweger

        [Home]
        6=Florian Nideröst
        9=Mikulas Komarek
        10=Oskar Henriksson
        12=Sandro Kellermüller
        14=Thomas Aellig
        46=David Holenstein
        00=Simon Suter
        TeamLong=UHC Uster
        TeamShort=UST
        Coach=Simon Meier
        Starting6=46,6,00,10,12,14
        Line1=77,53,21,20,19
        Line2=17,-,00,9,22
        Goal=46,-

        [Away]
        1=Janik Feiner
        4=Mario Jung
        TeamLong=Chur Unihockey
        TeamShort=CHUR
        Coach=Daniel Hahne
        Starting6=1,4,11,12,14,17
        Goal=1,4

        [Game-Config]
        [LastLineInConfig]
        """;

    [Fact]
    public void Parses_game_teams_and_lineups()
    {
        var game = GameConfigFile.Parse(Example);

        Assert.Equal("897281", game.Id);
        Assert.Equal(GameSource.File, game.Source);
        Assert.Equal("NLB", game.Title);
        Assert.Equal(new DateTime(2023, 6, 23, 20, 0, 0), game.Date);
        Assert.Equal("Buchholz, Uster", game.Location);
        Assert.Equal("Josef Fässler", game.Referee1);
        Assert.Equal("Raphi Berweger", game.Commentator2);

        Assert.Equal("UHC Uster", game.Home.LongName);
        Assert.Equal("UST", game.Home.ShortName);
        Assert.Equal("Simon Meier", game.Home.Coach);
        Assert.Equal(7, game.Home.Players.Count);
        Assert.Equal("Simon Suter", game.Home.Find(0)!.Name);
        Assert.Equal([46, 6, 0, 10, 12, 14], game.Home.Starting6);
        Assert.Equal(2, game.Home.Lines.Count);
        Assert.Equal([17, 0, 9, 22], game.Home.Lines[1]);
        Assert.Equal([46], game.Home.Goalies);

        Assert.Equal("Chur Unihockey", game.Away.LongName);
        Assert.Equal(2, game.Away.Players.Count);
        Assert.Equal([1, 4], game.Away.Goalies);
        Assert.False(game.Away.HasStarting6);
    }

    [Fact]
    public void Requires_a_game_number() =>
        Assert.Throws<ValidationException>(() => GameConfigFile.Parse("[Game]\nTitle=x\n[Home]\n5=A"));

    [Fact]
    public void Rejects_duplicate_numbers() =>
        Assert.Throws<ValidationException>(() => GameConfigFile.Parse("[Game]\nGameNr=1\n[Home]\n5=A\n5=B"));

    [Fact]
    public void Short_name_falls_back_to_initials()
    {
        var game = GameConfigFile.Parse("[Game]\nGameNr=1\n[Home]\nTeamLong=Red Ants Winterthur");
        Assert.Equal("RAW", game.Home.ShortName);
    }
}
