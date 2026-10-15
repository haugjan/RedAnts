using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Board;

public static class RecordDeckView
{
    public sealed record Command(string HallId, string DeckId, string Json);

    public sealed class Handler(IDeckMirror mirror, IHallRepository halls)
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
            var deck = hall.DeckLayouts.FirstOrDefault(d => d.Id == command.DeckId) ?? hall.DeckLayouts[0];
            mirror.Apply(command.HallId, command.DeckId, command.Json, deck);
        }
    }
}
