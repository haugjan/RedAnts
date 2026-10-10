namespace RedAnts.DJ.Features.Sounds;

public static class RestoreDJSound
{
    public sealed record Command(string BlobPath, Stream Content, string? ContentType);

    public sealed class Handler(IDJSoundUploader uploader)
    {
        public Task HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.BlobPath)) throw new DomainException("Ein Blob-Pfad ist erforderlich.");
            return uploader.UploadAtPathAsync(command.BlobPath, command.Content, command.ContentType);
        }
    }
}
