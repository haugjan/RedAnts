using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Games;

public static class SaveGame
{
    public sealed record Command(Game Game);

    public sealed class Handler(IGameRepository games, IHallReader halls, AgentSync sync)
    {
        public async Task<Game> HandleAsync(Command command)
        {
            var game = command.Game.Validated() with { LoadedAt = SwissTime.Timestamp };
            await games.SaveAsync(game);
            foreach (var hall in await halls.ListAsync())
                if (hall.ActiveGameId == game.Id) await sync.PushAsync(hall.Id);
            return game;
        }
    }
}
