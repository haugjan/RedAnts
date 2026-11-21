using RedAnts.Domain;
using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests;

public class GameReadyToSaveTests
{
    private static GameTeam Squad(params Player[] players) =>
        GameTeam.Empty(GameTeam.HomeColor) with { LongName = "Red Ants", ShortName = "RAW", Players = players };

    private static Game With(GameTeam home, GameTeam? away = null) =>
        Game.Manual("1", "Testspiel") with { Home = home, Away = away ?? GameTeam.Empty(GameTeam.AwayColor) };

    private static string Refused(Game game) =>
        Assert.Throws<ValidationException>(() => game.ReadyToSave()).Message;

    [Fact]
    public void A_squad_with_a_chosen_top_scorer_saves()
    {
        var game = With(Squad(new Player(7, "Nora Stark"), new Player(73, "Lea Suter")) with { TopScorer = 73 });

        Assert.Equal(73, game.ReadyToSave().Home.TopScorer);
    }

    [Fact]
    public void Without_a_top_scorer_nothing_is_saved() =>
        Assert.Contains($"{Game.TopScorerLabel} fehlt", Refused(With(Squad(new Player(7, "Nora Stark")))));

    [Fact]
    public void A_player_still_carrying_the_double_zero_blocks_the_save()
    {
        var message = Refused(With(Squad(new Player(0, "Annika Morf"), new Player(7, "Nora Stark")) with { TopScorer = 0 }));

        Assert.Contains("Annika Morf", message);
        Assert.Contains("Rückennummer", message);
    }

    [Fact]
    public void The_double_zero_is_reported_before_the_missing_choice()
    {
        var message = Refused(With(Squad(new Player(0, "Annika Morf"))));

        Assert.Contains("Annika Morf", message);
        Assert.DoesNotContain("fehlt", message);
    }

    [Fact]
    public void A_top_scorer_who_left_the_squad_blocks_the_save() =>
        Assert.Contains("steht nicht im Kader",
            Refused(With(Squad(new Player(7, "Nora Stark")) with { TopScorer = 99 })));

    [Fact]
    public void An_empty_squad_asks_for_nothing() =>
        With(GameTeam.Empty(GameTeam.HomeColor)).ReadyToSave();

    [Fact]
    public void Both_sides_are_checked()
    {
        var home = Squad(new Player(7, "Nora Stark")) with { TopScorer = 7 };
        var away = GameTeam.Empty(GameTeam.AwayColor) with { Players = [new Player(12, "Gast Zwoelf")] };

        Assert.Contains("Gast:", Refused(With(home, away)));
    }

    [Fact]
    public void The_old_rules_still_hold() =>
        Assert.Contains("doppelt",
            Refused(With(Squad(new Player(7, "A"), new Player(7, "B")) with { TopScorer = 7 })));
}
