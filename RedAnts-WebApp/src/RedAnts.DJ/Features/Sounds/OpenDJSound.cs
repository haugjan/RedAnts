namespace RedAnts.DJ.Features.Sounds;

public static class OpenDJSound
{
    public sealed record Query(string? BlobPath);

    public sealed class Handler(IDJSoundUploader uploader)
    {
        public Task<DJSoundContent?> HandleAsync(Query query) =>
            IsWithinContainer(query.BlobPath) ? uploader.OpenReadAsync(query.BlobPath!) : Task.FromResult<DJSoundContent?>(null);

        private static bool IsWithinContainer(string? path) =>
            !string.IsNullOrWhiteSpace(path)
            && !path.StartsWith('/')
            && !path.Contains('\\')
            && !path.Split('/').Any(segment => segment is "." or "..");
    }
}
