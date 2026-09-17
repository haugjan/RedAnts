using RedAnts.Show.Domain;
using RedAnts.Show.Features.Admin;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Sounds;
using Xunit;

namespace RedAnts.Show.Tests;

public class ShowSoundCleanupTests
{
    private static ShowProfile Sample()
    {
        var keep = new ShowButton("t1", "Keep",
            Sound: new ShowSound(SoundKind.Local, "sounds/keep.mp3"),
            Songs: new[] { new ShowSound(SoundKind.Local, "sounds/keep.mp3") });
        var pooled = new ShowButton("t2", "Pooled",
            Pool: new[] { new ShowSound(SoundKind.Local, "sounds/keep2.mp3") });
        var spotify = new ShowButton("t3", "Sp",
            Songs: new[] { new ShowSound(SoundKind.Spotify, "spotify:track:abc") });
        var sub = new ShowButton("f2", "Sub", Children: new[] { pooled });
        var folder = new ShowButton("f1", "Folder", Children: new[] { keep, sub, spotify });
        return new ShowProfile("p", "P", "#fff", new[] { folder });
    }

    private sealed class FakeProfiles(ShowProfile profile) : IShowProfiles
    {
        public Task<IReadOnlyList<ShowProfile>> GetAllAsync() => Task.FromResult<IReadOnlyList<ShowProfile>>(new[] { profile });
        public Task SaveAllAsync(IReadOnlyList<ShowProfile> profiles) => Task.CompletedTask;
    }

    private sealed class FakeUploader(List<ShowBlobInfo> blobs) : IShowSoundUploader
    {
        public readonly List<string> Deleted = new();
        public Task<IReadOnlyList<ShowBlobInfo>> ListAsync(string prefix) =>
            Task.FromResult<IReadOnlyList<ShowBlobInfo>>(blobs.Where(b => b.Path.StartsWith(prefix)).ToList());
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
        public Task<ShowSoundContent?> OpenReadAsync(string blobPath) => throw new NotSupportedException();
    }

    private static List<ShowBlobInfo> Blobs() => new()
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
