namespace RedAnts.Show.Features.Sounds;

public static class RestoreShowSound
{
    public sealed record Command(string BlobPath, Stream Content, string? ContentType);

    public sealed class Handler(IShowSoundUploader uploader)
    {
        public Task HandleAsync(Command command)
        {
            if (string.IsNullOrWhiteSpace(command.BlobPath)) throw new DomainException("Ein Blob-Pfad ist erforderlich.");
            return uploader.UploadAtPathAsync(command.BlobPath, command.Content, command.ContentType);
        }
    }
}
