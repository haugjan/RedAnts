using RedAnts.DJ.Domain;
using RedAnts.DJ.Features.Admin;
using RedAnts.DJ.Features.Board;
using RedAnts.DJ.Features.Sounds;
using Xunit;

namespace RedAnts.DJ.Tests;

public class DJSoundCleanupTests
{
    private static DJProfile Sample()
    {
        var keep = new DJButton("t1", "Keep",
            Sound: new DJSound(SoundKind.Local, "sounds/keep.mp3"),
            Songs: new[] { new DJSound(SoundKind.Local, "sounds/keep.mp3") });
        var pooled = new DJButton("t2", "Pooled",
            Pool: new[] { new DJSound(SoundKind.Local, "sounds/keep2.mp3") });
        var spotify = new DJButton("t3", "Sp",
            Songs: new[] { new DJSound(SoundKind.Spotify, "spotify:track:abc") });
        var sub = new DJButton("f2", "Sub", Children: new[] { pooled });
        var folder = new DJButton("f1", "Folder", Children: new[] { keep, sub, spotify });
        return new DJProfile("p", "P", "#fff", new[] { folder });
    }

    private sealed class FakeProfiles(DJProfile profile) : IDJProfiles
    {
        public Task<IReadOnlyList<DJProfile>> GetAllAsync() => Task.FromResult<IReadOnlyList<DJProfile>>(new[] { profile });
        public Task SaveAllAsync(IReadOnlyList<DJProfile> profiles) => Task.CompletedTask;
    }

    private sealed class FakeUploader(List<DJBlobInfo> blobs) : IDJSoundUploader
    {
        public readonly List<string> Deleted = new();
        public Task<IReadOnlyList<DJBlobInfo>> ListAsync(string prefix) =>
            Task.FromResult<IReadOnlyList<DJBlobInfo>>(blobs.Where(b => b.Path.StartsWith(prefix)).ToList());
        public Task<bool> DeleteAsync(string blobPath)
        {
            Deleted.Add(blobPath);
            var i = blobs.FindIndex(b => b.Path == blobPath);
            if (i < 0) return Task.FromResult(false);
            blobs.RemoveAt(i);
            return Task.FromResult(true);
        }
        public Task<string> UploadAsync(string fileName, Stream content, string? contentType) => throw new NotSupportedException();
        public Task UploadAtPathAsync(string blobPath, Stream content, string? contentType) => throw new NotSupportedException();
        public Task<byte[]?> DownloadAsync(string blobPath) => throw new NotSupportedException();
        public Task<DJSoundContent?> OpenReadAsync(string blobPath) => throw new NotSupportedException();
    }

    private static List<DJBlobInfo> Blobs() => new()
    {
        new("sounds/keep.mp3", 10, DateTimeOffset.UtcNow.AddDays(-5)),
        new("sounds/keep2.mp3", 20, DateTimeOffset.UtcNow.AddDays(-5)),
        new("sounds/orphan.mp3", 100, DateTimeOffset.UtcNow.AddDays(-5)),
        new("sounds/fresh.mp3", 200, DateTimeOffset.UtcNow),
    };

    [Fact]
    public async Task Find_reports_only_unreferenced_blobs_and_flags_recent()
    {
        var result = await new FindUnusedSounds.Handler(new FakeProfiles(Sample()), new FakeUploader(Blobs()))
            .HandleAsync(new FindUnusedSounds.Query());

        Assert.Equal(4, result.TotalBlobs);
        Assert.Equal(new[] { "sounds/fresh.mp3", "sounds/orphan.mp3" }, result.Unused.Select(u => u.Path).OrderBy(x => x));
        Assert.True(result.Unused.Single(u => u.Path == "sounds/fresh.mp3").RecentlyUploaded);
        Assert.False(result.Unused.Single(u => u.Path == "sounds/orphan.mp3").RecentlyUploaded);
        Assert.Equal(100, result.DeletableBytes);
    }

    [Fact]
    public async Task Delete_never_removes_a_referenced_blob()
    {
        var uploader = new FakeUploader(Blobs());
        var result = await new DeleteUnusedSounds.Handler(new FakeProfiles(Sample()), uploader)
            .HandleAsync(new DeleteUnusedSounds.Command(new[] { "sounds/orphan.mp3", "sounds/keep.mp3", "sounds/keep2.mp3" }));

        Assert.Equal(1, result.Deleted);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(100, result.FreedBytes);
        Assert.Equal(new[] { "sounds/orphan.mp3" }, uploader.Deleted);
    }
}
