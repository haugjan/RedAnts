namespace RedAnts.Show.Features.Music;

public static class UploadSound
{
    public sealed record Command(string FileName, Stream Content, string? ContentType);

    public sealed class Handler(ISoundStorage storage)
    {
        public Task<string> HandleAsync(Command command) =>
            storage.SaveAsync(command.FileName, command.Content, command.ContentType);
    }
}
