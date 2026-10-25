using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Tests;

public class OfflineGameTests
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 25, 12, 0, 0, TimeSpan.FromHours(2));

    private static Game Game(string id, DateTimeOffset at, string commentator = "") =>
        Cloud.Game.None with { Id = id, Title = "Red Ants – Piranha", LoadedAt = at, Commentator1 = commentator };

    private static AgentPayload Payload(Game game) =>
        new(Hall.Offline with { Id = "win4" }, game, DateTimeOffset.Now, []);

    private static PayloadStore Store(Game game)
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        store.Apply(Payload(game));
        return store;
    }

    [Fact]
    public void A_game_edited_in_the_hall_outlives_an_older_configuration_from_the_cloud()
    {
        var store = Store(Game("4711", Noon));

        store.ApplyGame(Game("4711", Noon.AddMinutes(5), "Nina Muster"));
        store.Apply(Payload(Game("4711", Noon)));

        Assert.Equal("Nina Muster", store.Game.Commentator1);
    }

    [Fact]
    public void A_newer_game_from_the_cloud_wins()
    {
        var store = Store(Game("4711", Noon));
        store.ApplyGame(Game("4711", Noon.AddMinutes(5), "Nina Muster"));

        store.Apply(Payload(Game("4711", Noon.AddMinutes(9), "Aus der Cloud")));

        Assert.Equal("Aus der Cloud", store.Game.Commentator1);
    }

    [Fact]
    public void The_outbox_holds_the_game_until_it_is_sent()
    {
        var outbox = new GameOutbox(NullLogger<GameOutbox>.Instance);
        var game = Game("4711", Noon.AddMinutes(5), "Nina Muster");

        outbox.Queue(game);
        var waiting = outbox.Pending;
        outbox.Sent(game);

        Assert.Equal("4711", waiting?.Id);
        Assert.Null(outbox.Pending);
    }

    [Fact]
    public void An_edit_made_while_the_older_one_waits_stays_in_the_outbox()
    {
        var outbox = new GameOutbox(NullLogger<GameOutbox>.Instance);
        var first = Game("4711", Noon.AddMinutes(5));
        var second = Game("4711", Noon.AddMinutes(7), "später");

        outbox.Queue(first);
        outbox.Queue(second);
        outbox.Sent(first);

        Assert.Equal("später", outbox.Pending?.Commentator1);
        outbox.Sent(second);
    }
}
