using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;

namespace RedAnts.Show.Agent.Tests;

public class TemplateTests
{
    static readonly Game Game = Game.None with
    {
        Title = "Red Ants - Kloten",
        Home = GameTeam.Empty with
        {
            LongName = "Red Ants Winterthur",
            ShortName = "RAW",
            Color = "#D02D38",
            Coach = "Trainerin",
            Players = [new Player(41, "Livia Werz", "Goalie"), new Player(7, "Nora Stark", "Stürmerin", Sponsor: "Kempa")],
            Starting6 = [41, 7],
            TopScorer = 7,
        },
        Away = GameTeam.Empty with { LongName = "Kloten-Dietlikon Jets", ShortName = "KDJ", Color = "#2A6EBB" },
        Referee1 = "M. Muster",
    };

    static LiveGame Live()
    {
        var live = new LiveGame();
        live.AdjustScore("home", 3);
        live.AdjustScore("away", 1);
        return live;
    }

    [Fact]
    public void A_player_fills_number_name_sponsor_and_the_team()
    {
        var live = Live();
        var ctx = new KeyContext("home", Game.Home.Find(7), Event: "goal");

        var text = Templates.Expand("{player.number} {player.name} ({player.sponsor}) {team.short} {score} {event} {referee1}", ctx, Game, live);

        Assert.Equal("7 Nora Stark (Kempa) RAW 3:1 goal M. Muster", text);
    }

    [Fact]
    public void The_side_decides_which_team_the_short_name_comes_from()
    {
        var live = Live();
        var ctx = new KeyContext("away");

        Assert.Equal("KDJ", Templates.Expand("{team.short}", ctx, Game, live));
        Assert.Equal("Red Ants Winterthur", Templates.Expand("{home.long}", ctx, Game, live));
    }

    [Fact]
    public void The_team_colour_goes_out_as_well()
    {
        Assert.Equal("#D02D38", Templates.Expand("{home.color}", new KeyContext("home"), Game, Live()));
        Assert.Equal("#2A6EBB", Templates.Expand("{away.color}", new KeyContext("home"), Game, Live()));
    }

    [Fact]
    public void The_topscorer_is_the_player_behind_the_entry()
    {
        var text = Templates.Expand("{topscorer.number} {topscorer.name}", new KeyContext("home"), Game, Live());

        Assert.Equal("7 Nora Stark", text);
    }

    [Fact]
    public void Penalty_and_free_text_come_from_the_key()
    {
        var ctx = new KeyContext("home", Penalty: "2+2", Text: "Hallo");

        Assert.Equal("2+2 Hallo", Templates.Expand("{penalty} {text}", ctx, Game, Live()));
    }

    [Fact]
    public void The_starting_six_can_be_read_position_by_position()
    {
        var text = Templates.Expand("{starting6.1.number} {starting6.1.name} / {starting6.2.number}", new KeyContext("home"), Game, Live());

        Assert.Equal("41 Livia Werz / 7", text);
    }

    [Fact]
    public void A_text_without_braces_stays_as_it_is()
    {
        Assert.Equal("Guten Abend", Templates.Expand("Guten Abend", KeyContext.None, Game, Live()));
    }
}
