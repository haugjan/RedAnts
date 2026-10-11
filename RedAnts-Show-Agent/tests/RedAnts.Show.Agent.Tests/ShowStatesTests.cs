using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Tests;

public class ShowStatesTests
{
    private static readonly IReadOnlyList<ShowState> Defaults =
    [
        new(ShowStates.Overlay, "Einblender", ["aus", "an"], "beobachtet"),
        new(ShowStates.Music, "Musik", ["aus", "läuft", "fade"], "beobachtet"),
        new(ShowStates.Led, "LED", ["aus", "läuft"], "gesetzt"),
        new(ShowStates.Sponsors, "Sponsoren-Loop", ["aus", "an"], "gesetzt"),
        new(ShowStates.ClockRunning, "Uhr läuft", ["steht", "läuft"], "beobachtet"),
    ];

    private static (ShowStates States, PayloadStore Store) Build(params AutomationRule[] rules)
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var hall = Hall.Offline with { Id = "win4", Automation = rules };
        store.Apply(new AgentPayload(hall, [], null, DateTimeOffset.Now, [], Defaults));
        return (new ShowStates(store, NullLogger<ShowStates>.Instance), store);
    }

    [Theory]
    [InlineData(SceneActions.OverlayShow, "title:player_event", ShowStates.Overlay, "an")]
    [InlineData(SceneActions.OverlayHide, "title:*", ShowStates.Overlay, "aus")]
    [InlineData(SceneActions.OverlayShow, "title:scoreboard", ShowStates.ClockVisible, "an")]
    [InlineData(SceneActions.SoundPlay, "sound:tor", ShowStates.Music, "läuft")]
    [InlineData(SceneActions.SoundFade, null, ShowStates.Music, "fade")]
    [InlineData(SceneActions.LedStart, "led:pause", ShowStates.Led, "läuft")]
    public void A_step_moves_the_state_it_touches(string action, string? target, string state, string value)
    {
        var effects = ShowStates.Effects(new SceneStep(action, target));

        Assert.Equal(value, effects[state]);
    }

    [Fact]
    public void The_sponsor_playlist_also_lights_the_sponsor_state()
    {
        var effects = ShowStates.Effects(new SceneStep(SceneActions.LedStart, "led:sponsoren"));

        Assert.Equal("läuft", effects[ShowStates.Led]);
        Assert.Equal("an", effects[ShowStates.Sponsors]);

        var stopped = ShowStates.Effects(new SceneStep(SceneActions.LedStop, "led:stopp"));
        Assert.Equal("aus", stopped[ShowStates.Led]);
        Assert.Equal("aus", stopped[ShowStates.Sponsors]);
    }

    [Fact]
    public void An_unset_state_reads_its_first_value()
    {
        var (states, _) = Build();

        Assert.Equal("aus", states.Value(ShowStates.Overlay));
        Assert.False(states.IsOn(ShowStates.Overlay));

        states.Set(ShowStates.Overlay, "an", "Test");

        Assert.Equal("an", states.Value(ShowStates.Overlay));
        Assert.True(states.IsOn(ShowStates.Overlay));
    }

    [Fact]
    public void A_rule_fires_once_when_its_state_reaches_the_value()
    {
        var (states, _) = Build(new AutomationRule(ShowStates.ClockRunning, "läuft", "sys-led-stopp"));
        var fired = new List<string>();
        states.RuleTriggered += (scene, _) => fired.Add(scene);

        states.Set(ShowStates.ClockRunning, "läuft", "Uhr");
        states.Set(ShowStates.ClockRunning, "läuft", "Uhr");
        states.Set(ShowStates.ClockRunning, "steht", "Uhr");

        Assert.Equal(["sys-led-stopp"], fired);
    }

    [Fact]
    public void A_rule_chain_stops_after_three_steps()
    {
        var (states, _) = Build(
            new AutomationRule(ShowStates.Overlay, "an", "endlos"),
            new AutomationRule(ShowStates.Led, "läuft", "endlos"));
        var fired = 0;
        states.RuleTriggered += (_, _) =>
        {
            fired++;
            states.Set(ShowStates.Led, fired % 2 == 0 ? "aus" : "läuft", "Kette");
        };

        states.Set(ShowStates.Overlay, "an", "Test");

        Assert.InRange(fired, 1, 4);
    }

    [Fact]
    public void Applying_a_step_sets_every_state_it_names()
    {
        var (states, _) = Build();

        states.Apply(new SceneStep(SceneActions.LedStart, "led:sponsoren"), "Szene");

        Assert.Equal("läuft", states.Value(ShowStates.Led));
        Assert.Equal("an", states.Value(ShowStates.Sponsors));
    }
}
