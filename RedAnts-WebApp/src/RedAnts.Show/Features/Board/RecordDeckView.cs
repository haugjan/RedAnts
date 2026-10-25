using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Setup;

namespace RedAnts.Show.Features.Board;

public static class RecordDeckView
{
    public sealed record Command(string HallId, string DeckId, string Json);

    public sealed class Handler(IDeckMirror mirror, IHallRepository halls, IShowSetup setup)
    {
        public async Task HandleAsync(Command command)
        {
            if (command.Json.Length == 0)
            {
                mirror.Drop(command.HallId);
                return;
            }
            if (command.DeckId.Length == 0)
            {
                mirror.ApplyLive(command.HallId, command.Json);
                return;
            }
            if (await halls.GetByIdAsync(command.HallId) is not { } hall) return;
            var decks = (await setup.GetAsync()).DecksOf(hall);
            if ((decks.FirstOrDefault(d => d.Id == command.DeckId) ?? decks.FirstOrDefault()) is not { } deck) return;
            mirror.Apply(command.HallId, command.DeckId, command.Json, deck);
        }
    }
}
