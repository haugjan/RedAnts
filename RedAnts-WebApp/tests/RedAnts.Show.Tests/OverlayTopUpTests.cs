using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests;

public class OverlayTopUpTests
{
    private static ShowSetup With(params LedTemplate[] templates) => new([], templates);

    private static string Command(ShowSetup show, string name) =>
        show.Led.Single(t => t.Event == name).Command;

    [Fact]
    public void The_break_picture_becomes_an_overlay_so_the_clock_survives()
    {
        var filled = OverlayTopUp.Fill(With(new LedTemplate("pause", "start playlist -n \"STR_RA_Pause\"")));

        Assert.Equal("start playlist -n \"STR_RA_Pause\" -o", Command(filled!, "pause"));
    }

    [Fact]
    public void The_intro_is_treated_the_same()
    {
        var filled = OverlayTopUp.Fill(With(new LedTemplate("intro", "start playlist -n \"STR_RA_Intro\"")));

        Assert.Equal("start playlist -n \"STR_RA_Intro\" -o", Command(filled!, "intro"));
    }

    [Fact]
    public void A_template_that_already_overlays_is_left_alone() =>
        Assert.Null(OverlayTopUp.Fill(With(new LedTemplate("pause", "start playlist -n \"STR_RA_Pause\" -o"))));

    [Fact]
    public void The_game_picture_keeps_the_main_layer() =>
        Assert.Null(OverlayTopUp.Fill(With(new LedTemplate("spiel-nla", "start playlist -n \"STR_Default_Game_NLA\""))));

    [Fact]
    public void A_stop_command_is_not_given_the_flag() =>
        Assert.Null(OverlayTopUp.Fill(With(new LedTemplate("pause", "stop playlist"))));

    [Fact]
    public void Only_the_named_events_are_touched()
    {
        var filled = OverlayTopUp.Fill(With(
            new LedTemplate("pause", "start playlist -n \"STR_RA_Pause\""),
            new LedTemplate("pausenspiel", "start playlist -n \"STR_RA_Pausenspiel\"")));

        Assert.EndsWith("-o", Command(filled!, "pause"));
        Assert.DoesNotContain("-o", Command(filled, "pausenspiel"));
    }

    [Fact]
    public void The_shipped_defaults_already_overlay_both()
    {
        foreach (var name in OverlayTopUp.Events)
            Assert.EndsWith("-o", Hall.DefaultLedTemplates.Single(t => t.Event == name).Command);
    }
}
