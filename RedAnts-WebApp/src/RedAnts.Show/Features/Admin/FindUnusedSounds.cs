using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Sounds;

namespace RedAnts.Show.Features.Admin;

public static class FindUnusedSounds
{
    public sealed record Query;

    public sealed record UnusedSound(string Path, long SizeBytes, DateTimeOffset? LastModified, bool RecentlyUploaded);

    public sealed record Result(IReadOnlyList<UnusedSound> Unused, int TotalBlobs, int ReferencedCount, long DeletableBytes);

    public static readonly TimeSpan RecentWindow = TimeSpan.FromHours(1);

    public sealed class Handler(IShowProfiles profiles, IShowSoundUploader uploader)
    {
        public async Task<Result> HandleAsync(Query query)
        {
            var referenced = ShowSoundReferences.Collect(await profiles.GetAllAsync());
            var blobs = await uploader.ListAsync("sounds/");
            var cutoff = DateTimeOffset.UtcNow - RecentWindow;
            var unused = blobs
                .Where(b => !referenced.Contains(b.Path))
                .OrderByDescending(b => b.SizeBytes)
                .Select(b => new UnusedSound(b.Path, b.SizeBytes, b.LastModified, b.LastModified is { } lm && lm > cutoff))
                .ToList();
            var deletableBytes = unused.Where(u => !u.RecentlyUploaded).Sum(u => u.SizeBytes);
            return new Result(unused, blobs.Count, referenced.Count, deletableBytes);
        }
    }
}
