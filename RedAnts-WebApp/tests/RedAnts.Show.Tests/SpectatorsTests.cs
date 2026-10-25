using RedAnts.Show.Domain;
using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Halls;
using Xunit;

namespace RedAnts.Show.Tests;

public class SpectatorsTests
{
    private sealed class Halls(Hall hall) : IHallRepository
    {
        public Task<Hall?> GetByIdAsync(string id) => Task.FromResult(hall.Id == id ? hall : null);
        public Task SaveAsync(Hall saved) { hall = saved; return Task.CompletedTask; }
        public Task DeleteAsync(string id) => Task.CompletedTask;
    }

    private sealed class Games : IGameRepository
    {
        public Game? Stored { get; set; }
        public int Writes { get; private set; }
        public Task<Game?> GetByIdAsync(string id) => Task.FromResult(Stored?.Id == id ? Stored : null);
        public Task SaveAsync(Game game) { Stored = game; Writes++; return Task.CompletedTask; }
        public Task DeleteAsync(string id) => Task.CompletedTask;
    }

    private static (RecordSpectators.Handler Handler, Games Games) Build(string? activeGame = "4711")
    {
        var hall = Hall.Create("win4", "WIN4 Stratos") with { ActiveGameId = activeGame };
        var games = new Games { Stored = Game.Manual("4711", "Red Ants – Piranha") };
        return (new RecordSpectators.Handler(new Halls(hall), games), games);
    }

    [Fact]
    public async Task The_count_of_the_hall_lands_on_its_active_game()
    {
        var (handler, games) = Build();

        await handler.HandleAsync(new RecordSpectators.Command("win4", 412));

        Assert.Equal(412, games.Stored!.Spectators);
    }

    [Fact]
    public async Task The_same_count_twice_writes_once()
    {
        var (handler, games) = Build();

        await handler.HandleAsync(new RecordSpectators.Command("win4", 412));
        await handler.HandleAsync(new RecordSpectators.Command("win4", 412));

        Assert.Equal(1, games.Writes);
    }

    [Fact]
    public async Task A_hall_without_an_active_game_counts_nothing()
    {
        var (handler, games) = Build(activeGame: null);

        await handler.HandleAsync(new RecordSpectators.Command("win4", 412));

        Assert.Equal(0, games.Writes);
    }

    [Fact]
    public async Task A_negative_count_is_ignored()
    {
        var (handler, games) = Build();

        await handler.HandleAsync(new RecordSpectators.Command("win4", -5));

        Assert.Equal(0, games.Writes);
    }

    [Fact]
    public void Reloading_a_game_from_swissunihockey_keeps_the_count()
    {
        var counted = Game.Manual("4711", "Red Ants – Piranha") with { Spectators = 412 };

        var merged = counted.Merge(Game.Manual("4711", "Red Ants – Piranha SC"));

        Assert.Equal(412, merged.Spectators);
    }
}
