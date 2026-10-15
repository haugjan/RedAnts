using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Games;

public static class LoadGameFromSwissUnihockey
{
    public sealed record Command(string HallId, string GameId);

    public sealed class Handler(IHallRepository halls, IGameRepository games, ISwissUnihockeyGames swissUnihockey, AgentSync sync)
    {
        public async Task<Game> HandleAsync(Command command)
        {
            var hall = await halls.GetByIdAsync(command.HallId)
                ?? throw new DomainException($"Halle „{command.HallId}“ nicht gefunden.");
            var fresh = await swissUnihockey.LoadAsync(command.GameId);
            var stored = await games.GetByIdAsync(fresh.Id);
            var game = (stored is null ? fresh : stored.Merge(fresh)).Validated();
            await games.SaveAsync(game);
            await halls.SaveAsync(hall.WithActiveGame(game.Id));
            await sync.PushAsync(hall.Id);
            return game;
        }
    }
}
