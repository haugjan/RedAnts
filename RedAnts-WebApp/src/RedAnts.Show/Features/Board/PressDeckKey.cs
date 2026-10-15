namespace RedAnts.Show.Features.Board;

public static class PressDeckKey
{
    public sealed record Command(string HallId, string DeckId, int Number);

    public sealed class Handler(IDeckMirror mirror)
    {
        public Task<bool> HandleAsync(Command command) =>
            mirror.PressAsync(command.HallId, command.DeckId, command.Number);
    }
}
