using RedAnts.Show.Domain;
using RedAnts.Show.Features.Board;
using Xunit;

namespace RedAnts.Show.Tests.Board;

public class BoardPreviewTests
{
    static readonly BoardFunctions All = new(true, true, true, true, true);

    static ButtonAction Steps(params KeyStep[] steps) => new(KeyKinds.Steps, Steps: steps);

    [Fact]
    public void A_goal_from_the_speaker_shows_the_scorer_counts_the_goal_and_puts_her_number_on_the_led()
    {
        var goal = new ButtonAction(KeyKinds.Special, Special: SpecialKey.GoalHome);
        var pressed = BoardPreview.Start.Press(goal, All);
        var preview = pressed.Picked(goal, new Player(23, "Anna Beispiel"), All);

        Assert.True(BoardPreview.Picks(goal));
        Assert.Null(pressed.Title);
        Assert.Equal("player_event", preview.Title?.Kind);
        Assert.Equal("Anna Beispiel", preview.Title?.Name);
        Assert.Equal("23", preview.Title?.Number);
        Assert.Equal("TOR", preview.Title?.Event);
        Assert.Equal(1, preview.Home);
        Assert.Equal("tor", preview.Led);
    }

    [Fact]
    public void A_guest_goal_stays_off_the_led_wall()
    {
        var goal = new ButtonAction(KeyKinds.Special, Special: SpecialKey.GoalAway);
        var preview = BoardPreview.Start.Picked(goal, BoardPreview.SamplePlayer("away"), All);

        Assert.Equal(1, preview.Away);
        Assert.Null(preview.Led);
    }

    [Fact]
    public void Led_steps_change_the_led_and_stop_goes_back_to_the_screen()
    {
        var on = BoardPreview.Start.Press(Steps(new KeyStep(KeyActions.LedStart, "led:intro")), All);
        var off = on.Press(Steps(new KeyStep(KeyActions.LedStop)), All);

        Assert.Equal("intro", on.Led);
        Assert.Null(off.Led);
    }

    [Fact]
    public void A_switched_off_function_leaves_its_screen_alone()
    {
        var noLed = All with { Led = false, Livestream = false };

        var preview = BoardPreview.Start.Press(Steps(
            new KeyStep(KeyActions.LedStart, "led:intro"),
            new KeyStep(KeyActions.VmixShow, "title:timeout", Side: "home")), noLed);

        Assert.Null(preview.Led);
        Assert.Null(preview.Title);
    }

    [Fact]
    public void A_title_shows_until_the_wait_is_over()
    {
        var action = Steps(
            new KeyStep(KeyActions.VmixShow, "title:timeout", Side: "away"),
            new KeyStep(KeyActions.Wait, Ms: 5000),
            new KeyStep(KeyActions.VmixHide, "title:timeout"));

        var shown = BoardPreview.Start.Press(action, All);
        var phases = BoardPreview.Phases(action);
        var hidden = shown.Run(phases[1].Steps, All);

        Assert.Equal("timeout", shown.Title?.Kind);
        Assert.Equal("Gegner", shown.Title?.Info);
        Assert.Equal(5000, phases[1].Ms);
        Assert.Null(hidden.Title);
    }

    [Fact]
    public void The_clock_key_hides_only_the_livestream_scoreboard() =>
        Assert.False(BoardPreview.Start.Press(new ButtonAction(KeyKinds.Special, Special: SpecialKey.ClockToggle), All).Scoreboard);

    [Fact]
    public void Starting_six_of_the_guests_shows_their_logo_on_the_led() =>
        Assert.Equal("gast", BoardPreview.Start.Picked(new ButtonAction(KeyKinds.Special, Special: SpecialKey.Starting6Away),
            BoardPreview.SamplePlayer("away"), All).Led);

    [Fact]
    public void A_penalty_shows_the_chosen_length() =>
        Assert.Equal("5'", BoardPreview.Start.Picked(new ButtonAction(KeyKinds.Special, Special: SpecialKey.PenaltyAway),
            BoardPreview.SamplePlayer("away"), All, "5'").Title?.Event);

    [Fact]
    public void Red_ants_are_always_the_left_side_even_when_they_play_away()
    {
        var game = new Game("1", GameSource.Manual, "", "", null, "",
            GameTeam.Empty(GameTeam.HomeColor) with { LongName = "UHC Gegnerinnen", Players = [new Player(4, "Gast")] },
            GameTeam.Empty(GameTeam.AwayColor) with { LongName = "Red Ants Rychenberg Winterthur", Players = [new Player(17, "Heim")] },
            "", "", "", "", DateTimeOffset.UnixEpoch);

        Assert.Equal("Red Ants Rychenberg Winterthur", PreviewTeams.From(game).Home);
        Assert.Equal("Heim", PreviewTeams.Team(game, "home").Players[0].Name);
    }

    [Theory]
    [InlineData("player_event", "number2")]
    [InlineData("starting6", "number1")]
    [InlineData("result", "card")]
    [InlineData("timeout", "lines2")]
    [InlineData("message", "lines1")]
    public void Each_title_uses_its_swiss_unihockey_template(string kind, string template) =>
        Assert.Equal(template, BoardPreview.TemplateOf(BoardPreview.Start.TitleOf(kind, "home")));
}
