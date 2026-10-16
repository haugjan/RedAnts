using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Tests;

public class ShowStatesTests
{
    private static ShowStates Build()
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        store.Apply(new AgentPayload(Hall.Offline with { Id = "win4" }, null, DateTimeOffset.Now, []));
        return new ShowStates(NullLogger<ShowStates>.Instance);
    }

    [Fact]
    public void Every_state_starts_on_its_first_value()
    {
        var states = Build();

        foreach (var state in ShowStates.Known)
            Assert.Equal(state.First, states.Value(state.Id));
    }

    [Fact]
    public void A_state_keeps_what_was_set_and_tells_what_it_started_from()
    {
        var states = Build();

        states.Set(ShowStates.Music, "läuft", "Test");

        Assert.Equal("läuft", states.Value(ShowStates.Music));
        Assert.Equal("aus", states.FirstValue(ShowStates.Music));
    }

    [Fact]
    public void Setting_the_same_value_twice_announces_a_change_only_once()
    {
        var states = Build();
        var changes = 0;
        states.Changed += () => changes++;

        states.Set(ShowStates.Led, "läuft", "Test");
        states.Set(ShowStates.Led, "läuft", "Test");

        Assert.Equal(1, changes);
    }

    [Fact]
    public void An_unknown_state_has_no_value_and_no_first_value()
    {
        var states = Build();

        Assert.Equal("", states.Value("gibt-es-nicht"));
        Assert.Equal("", states.FirstValue("gibt-es-nicht"));
    }

    [Fact]
    public void The_states_the_board_shows_are_the_ones_the_hall_control_knows()
    {
        Assert.Contains(ShowStates.Known, s => s.Id == ShowStates.Overlay);
        Assert.Contains(ShowStates.Known, s => s.Id == ShowStates.ClockVisible);
        Assert.Contains(ShowStates.Known, s => s.Id == ShowStates.ClockRunning);
        Assert.Contains(ShowStates.Known, s => s.Id == ShowStates.Music);
        Assert.Contains(ShowStates.Known, s => s.Id == ShowStates.AutoBreakMusic);
    }
}
