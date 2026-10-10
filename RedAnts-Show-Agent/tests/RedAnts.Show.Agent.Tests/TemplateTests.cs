using RedAnts.Show.Agent;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Scenes;

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
            Coach = "Trainerin",
            Players = [new Player(41, "Livia Werz", "Goalie"), new Player(7, "Nora Stark", "Stürmerin", Sponsor: "Kempa")],
            Starting6 = [41, 7],
        },
        Away = GameTeam.Empty with { LongName = "Kloten-Dietlikon Jets", ShortName = "KDJ" },
        Referee1 = "Hans Muster",
    };

    [Fact]
    public void Expands_player_team_and_score_placeholders()
    {
        var live = new LiveGame();
        live.AdjustScore("home", 2);
        var scene = new Scene("tor-heim", "Tor Heim", "home", []);
        var ctx = SceneContext.From(scene, new Dictionary<string, string> { ["player"] = "7", ["event"] = "goal" }, Game, live);

        var text = Templates.Expand("{player.number} {player.name} ({player.sponsor}) {team.short} {score} {event} {referee1}", ctx, Game, live);

        Assert.Equal("7 Nora Stark (Kempa) RAW 2:0 goal Hans Muster", text);
    }

    [Fact]
    public void Side_argument_overrides_the_scene_side()
    {
        var live = new LiveGame();
        var scene = new Scene("name", "Name", "home", []);
        var ctx = SceneContext.From(scene, new Dictionary<string, string> { ["side"] = "away" }, Game, live);

        Assert.Equal("KDJ", Templates.Expand("{team.short}", ctx, Game, live));
        Assert.Equal("Red Ants Winterthur", Templates.Expand("{home.long}", ctx, Game, live));
    }

    [Fact]
    public void Penalty_and_text_arguments_are_available()
    {
        var live = new LiveGame();
        var scene = new Scene("strafe-heim", "Strafe", "home", []);
        var ctx = SceneContext.From(scene, new Dictionary<string, string> { ["penalty"] = "2+2", ["text"] = "Hallo" }, Game, live);

        Assert.Equal("2+2 Hallo", Templates.Expand("{penalty} {text}", ctx, Game, live));
    }

    [Fact]
    public void Starting6_lists_number_and_name()
    {
        var live = new LiveGame();
        var ctx = SceneContext.From(new Scene("s6", "S6", "home", []), new Dictionary<string, string>(), Game, live);

        Assert.Equal("41 Livia Werz, 7 Nora Stark", Templates.Expand("{team.starting6}", ctx, Game, live));
    }

    [Fact]
    public void Nested_context_keeps_the_picked_player()
    {
        var live = new LiveGame();
        var ctx = SceneContext.From(new Scene("outer", "Outer", "home", []), new Dictionary<string, string> { ["player"] = "41" }, Game, live);

        var nested = ctx.Nested(new Scene("inner", "Inner", "", []));

        Assert.Equal("home", nested.Side);
        Assert.Equal(41, nested.Player!.Number);
    }
}
