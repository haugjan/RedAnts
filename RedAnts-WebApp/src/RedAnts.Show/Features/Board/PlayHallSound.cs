namespace RedAnts.Show.Features.Board;

public static class PlayHallSound
{
    public sealed record Command(string HallId, string SoundId);

    public sealed class Handler(IDeckMirror mirror)
    {
        public Task<bool> HandleAsync(Command command) =>
            mirror.SoundAsync(command.HallId, command.SoundId);
    }
}
