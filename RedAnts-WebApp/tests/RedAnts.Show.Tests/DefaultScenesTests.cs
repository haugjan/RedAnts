using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests;

public class DefaultScenesTests
{
    [Fact]
    public void Ids_are_unique_and_every_step_is_a_known_action()
    {
        var ids = DefaultScenes.All.Select(s => s.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        foreach (var scene in DefaultScenes.All)
            Assert.Same(scene, scene.Validated());
    }

    [Fact]
    public void Goal_scene_picks_the_player_before_showing_the_overlay()
    {
        var goal = DefaultScenes.All.Single(s => s.Id == "tor-heim");
        var actions = goal.Steps.Select(s => s.Action).ToList();
        Assert.True(actions.IndexOf(SceneActions.PlayerPick) < actions.IndexOf(SceneActions.OverlayShow));
        Assert.True(actions.IndexOf(SceneActions.SoundPlay) < actions.IndexOf(SceneActions.PlayerPick));
        Assert.Equal("home", goal.Side);
    }

    [Theory]
    [InlineData("strafe-heim", "PlayerHome")]
    [InlineData("strafe-gast", "PlayerAway")]
    public void Penalty_scenes_open_the_player_page_before_waiting_for_a_pick(string id, string context)
    {
        var scene = DefaultScenes.All.Single(s => s.Id == id);
        var actions = scene.Steps.Select(s => s.Action).ToList();
        Assert.Equal(context, scene.Steps.First(s => s.Action == SceneActions.DeckContext).Target);
        Assert.True(actions.IndexOf(SceneActions.DeckContext) < actions.IndexOf(SceneActions.PlayerPick));
    }

    [Fact]
    public void System_scenes_are_fixed_and_come_on_top_of_the_stored_ones()
    {
        var all = DefaultScenes.WithSystem(DefaultScenes.All);

        Assert.All(DefaultScenes.System, s => Assert.True(s.System));
        Assert.DoesNotContain(DefaultScenes.All, s => s.System);
        Assert.Equal(DefaultScenes.All.Count + DefaultScenes.System.Count, all.Count);
        Assert.Equal(DefaultScenes.System.Select(s => s.Id), all.Where(s => s.System).Select(s => s.Id));
    }

    [Fact]
    public void Scene_effects_follow_from_its_steps()
    {
        var goal = DefaultScenes.All.Single(s => s.Id == "tor-heim").Effects;

        Assert.Equal("läuft", goal[DefaultStates.Music]);
        Assert.Equal("läuft", goal[DefaultStates.Led]);
        Assert.Equal("aus", goal[DefaultStates.Overlay]);

        var sponsors = DefaultScenes.All.Single(s => s.Id == "sponsoren").Effects;
        Assert.Equal("an", sponsors[DefaultStates.Sponsors]);

        var clockOff = DefaultScenes.System.Single(s => s.Id == "sys-uhr-aus").Effects;
        Assert.Equal("aus", clockOff[DefaultStates.ClockVisible]);
        Assert.False(clockOff.ContainsKey(DefaultStates.Overlay));
    }

    [Fact]
    public void Panic_hides_everything_and_returns_to_main()
    {
        var panic = DefaultScenes.System.Single(s => s.Id == "panic");
        Assert.Contains(panic.Steps, s => s.Action == SceneActions.OverlayHide && s.Target == "title:*");
        Assert.Contains(panic.Steps, s => s.Action == SceneActions.SoundStop);
        Assert.Equal("Main", panic.Steps.Last().Target);
    }
}
