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

    [Fact]
    public void Panic_hides_everything_and_returns_to_main()
    {
        var panic = DefaultScenes.All.Single(s => s.Id == "panic");
        Assert.Contains(panic.Steps, s => s.Action == SceneActions.OverlayHide && s.Target == "title:*");
        Assert.Contains(panic.Steps, s => s.Action == SceneActions.SoundStop);
        Assert.Equal("Main", panic.Steps.Last().Target);
    }
}
