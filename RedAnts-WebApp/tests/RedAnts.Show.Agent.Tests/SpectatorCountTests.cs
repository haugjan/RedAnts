using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Tests;

public class SpectatorCountTests
{
    private static Game Game(string id) => Cloud.Game.None with { Id = id, Title = "Red Ants – Piranha" };

    private static AgentPayload Payload(Game game) =>
        new(Hall.Offline with { Id = "win4" }, game, DateTimeOffset.Now, []);

    private static PayloadStore Store(Game game)
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        store.Apply(Payload(game));
        return store;
    }

    [Fact]
    public void The_count_belongs_to_the_game()
    {
        var store = Store(Game("4711"));

        store.SetSpectators(412);

        Assert.Equal(412, store.Game.Spectators);
    }

    [Fact]
    public void A_configuration_from_the_cloud_does_not_reset_the_running_count()
    {
        var store = Store(Game("4711"));
        store.SetSpectators(412);

        store.Apply(Payload(Game("4711")));

        Assert.Equal(412, store.Game.Spectators);
    }

    [Fact]
    public void The_next_game_starts_at_zero()
    {
        var store = Store(Game("4711"));
        store.SetSpectators(412);

        store.Apply(Payload(Game("4712")));

        Assert.Equal(0, store.Game.Spectators);
    }
}
