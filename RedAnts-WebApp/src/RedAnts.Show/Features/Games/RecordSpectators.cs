using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Games;

public static class RecordSpectators
{
    public sealed record Command(string HallId, int Count);

    public sealed class Handler(IHallRepository halls, IGameRepository games)
    {
        public async Task HandleAsync(Command command)
        {
            if (command.Count < 0) return;
            if (await halls.GetByIdAsync(command.HallId) is not { ActiveGameId: { Length: > 0 } id }) return;
            if (await games.GetByIdAsync(id) is not { } game || game.Spectators == command.Count) return;
            await games.SaveAsync(game with { Spectators = command.Count });
        }
    }
}
