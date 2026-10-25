using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Features.Setup;
using Xunit;

namespace RedAnts.Show.Tests;

public class GameFromAgentTests
{
    private sealed class Halls(Hall hall) : IHallRepository
    {
        public Task<Hall?> GetByIdAsync(string id) => Task.FromResult(hall.Id == id ? hall : null);
        public Task SaveAsync(Hall saved) => Task.CompletedTask;
        public Task DeleteAsync(string id) => Task.CompletedTask;
    }

    private sealed class Games(Game stored) : IGameRepository
    {
        public Game Stored { get; private set; } = stored;
        public int Writes { get; private set; }
        public Task<Game?> GetByIdAsync(string id) => Task.FromResult(Stored.Id == id ? Stored : null);
        public Task SaveAsync(Game game) { Stored = game; Writes++; return Task.CompletedTask; }
        public Task DeleteAsync(string id) => Task.CompletedTask;
    }

    private sealed class NoHalls : IHallReader
    {
        public Task<IReadOnlyList<HallRow>> ListAsync() => Task.FromResult<IReadOnlyList<HallRow>>([]);
    }

    private sealed class NoMusic : IMusicLibrary
    {
        public Task<IReadOnlyList<MusicItem>> AllAsync() => Task.FromResult<IReadOnlyList<MusicItem>>([]);
        public Task<IReadOnlyList<MusicNode>> TreeAsync() => Task.FromResult<IReadOnlyList<MusicNode>>([]);
    }

    private sealed class Setup : IShowSetup
    {
        public Task<ShowSetup> GetAsync() => Task.FromResult(ShowSetup.Default);
        public Task SaveAsync(ShowSetup setup) => Task.CompletedTask;
    }

    private sealed class Silent : IAgentChannel, IAgentConnections
    {
        public Task SyncAsync(string hallId, AgentPayload payload) => Task.CompletedTask;
        public Task PanicAsync(string hallId) => Task.CompletedTask;
        public void Register(AgentConnection connection) { }
        public string? Remove(string connectionId) => null;
        public bool IsConnected(string hallId) => false;
        public AgentConnection? Of(string hallId) => null;
    }

    private static Game Loaded() =>
        Game.Manual("4711", "Red Ants – Piranha") with { Commentator1 = "alt" };

    private static (SaveGameFromAgent.Handler Handler, Games Games) Build(string? activeGame = "4711")
    {
        var hall = Hall.Create("win4", "WIN4 Stratos") with { ActiveGameId = activeGame };
        var games = new Games(Loaded());
        var halls = new Halls(hall);
        var silent = new Silent();
        var sync = new AgentSync(halls, new NoHalls(), games, new NoMusic(), new Setup(), silent, silent);
        return (new SaveGameFromAgent.Handler(halls, games, sync, NullLogger<SaveGameFromAgent.Handler>.Instance), games);
    }

    private static string Json(Game game) => JsonSerializer.Serialize(game, ShowJson.Options);

    [Fact]
    public async Task The_game_edited_in_the_hall_lands_in_the_cloud()
    {
        var (handler, games) = Build();
        var edited = Loaded() with { Commentator1 = "Nina Muster" };

        await handler.HandleAsync(new SaveGameFromAgent.Command("win4", Json(edited)));

        Assert.Equal("Nina Muster", games.Stored.Commentator1);
    }

    [Fact]
    public async Task A_game_the_hall_does_not_play_is_ignored()
    {
        var (handler, games) = Build();
        var foreign = Game.Manual("9999", "Fremdes Spiel");

        await handler.HandleAsync(new SaveGameFromAgent.Command("win4", Json(foreign)));

        Assert.Equal(0, games.Writes);
    }

    [Fact]
    public async Task A_hall_without_an_active_game_is_ignored()
    {
        var (handler, games) = Build(activeGame: null);

        await handler.HandleAsync(new SaveGameFromAgent.Command("win4", Json(Loaded())));

        Assert.Equal(0, games.Writes);
    }

    [Fact]
    public async Task Rubbish_from_the_hall_does_not_throw()
    {
        var (handler, games) = Build();

        await handler.HandleAsync(new SaveGameFromAgent.Command("win4", "kein json"));

        Assert.Equal(0, games.Writes);
    }

    [Fact]
    public async Task A_roster_with_the_same_number_twice_is_refused()
    {
        var (handler, games) = Build();
        var broken = Loaded() with
        {
            Home = GameTeam.Empty(GameTeam.HomeColor) with
            {
                Players = [new Player(7, "Eine"), new Player(7, "Andere")],
            },
        };

        await handler.HandleAsync(new SaveGameFromAgent.Command("win4", Json(broken)));

        Assert.Equal(0, games.Writes);
    }
}
