namespace RedAnts.Show.Features.Board;

public static class PlayHallMusic
{
    public sealed record Command(string HallId, string MusicId);

    public sealed class Handler(IDeckMirror mirror)
    {
        public Task<bool> HandleAsync(Command command) =>
            mirror.MusicAsync(command.HallId, command.MusicId);
    }
}
