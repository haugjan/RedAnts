namespace RedAnts.DJ.Features.Sounds;

public static class DownloadDJSound
{
    public sealed record Query(string BlobPath);

    public sealed class Handler(IDJSoundUploader uploader)
    {
        public Task<byte[]?> HandleAsync(Query query) =>
            string.IsNullOrWhiteSpace(query.BlobPath) ? Task.FromResult<byte[]?>(null) : uploader.DownloadAsync(query.BlobPath);
    }
}
