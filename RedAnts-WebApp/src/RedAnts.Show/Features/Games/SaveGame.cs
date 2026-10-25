using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.Games;

public static class SaveGame
{
    public sealed record Command(Game Game);

    public sealed class Handler(IGameRepository games, AgentSync sync)
    {
        public async Task<Game> HandleAsync(Command command)
        {
            var game = command.Game.Validated() with { LoadedAt = SwissTime.Timestamp };
            await games.SaveAsync(game);
            await sync.PushGameAsync(game.Id);
            return game;
        }
    }
}
