namespace RedAnts.Show.Features.Sounds;

public static class DownloadShowSound
{
    public sealed record Query(string BlobPath);

    public sealed class Handler(IShowSoundUploader uploader)
    {
        public Task<byte[]?> HandleAsync(Query query) =>
            string.IsNullOrWhiteSpace(query.BlobPath) ? Task.FromResult<byte[]?>(null) : uploader.DownloadAsync(query.BlobPath);
    }
}
