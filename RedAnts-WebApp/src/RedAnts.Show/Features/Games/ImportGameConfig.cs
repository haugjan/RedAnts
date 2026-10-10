using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Games;

public static class ImportGameConfig
{
    public sealed record Command(string HallId, string Text);

    public sealed class Handler(IHallRepository halls, IGameRepository games, AgentSync sync)
    {
        public async Task<Game> HandleAsync(Command command)
        {
            var hall = await halls.GetByIdAsync(command.HallId)
                ?? throw new DomainException($"Halle „{command.HallId}“ nicht gefunden.");
            var game = GameConfigFile.Parse(command.Text);
            await games.SaveAsync(game);
            await halls.SaveAsync(hall.WithActiveGame(game.Id));
            await sync.PushAsync(hall.Id);
            return game;
        }
    }
}
