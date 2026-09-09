namespace RedAnts.Show.Features.Sounds;

public static class OpenShowSound
{
    public sealed record Query(string? BlobPath);

    public sealed class Handler(IShowSoundUploader uploader)
    {
        public Task<ShowSoundContent?> HandleAsync(Query query) =>
            IsWithinContainer(query.BlobPath) ? uploader.OpenReadAsync(query.BlobPath!) : Task.FromResult<ShowSoundContent?>(null);

        private static bool IsWithinContainer(string? path) =>
            !string.IsNullOrWhiteSpace(path)
            && !path.StartsWith('/')
            && !path.Contains('\\')
            && !path.Split('/').Any(segment => segment is "." or "..");
    }
}
