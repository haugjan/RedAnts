namespace RedAnts.Show.Features.Sounds;

public sealed record ShowSoundContent(Stream Content, string ContentType, DateTimeOffset? LastModified, string? ETag);

public sealed record ShowBlobInfo(string Path, long SizeBytes, DateTimeOffset? LastModified);

public interface IShowSoundUploader
{
    Task<string> UploadAsync(string fileName, Stream content, string? contentType);
    Task UploadAtPathAsync(string blobPath, Stream content, string? contentType);
    Task<byte[]?> DownloadAsync(string blobPath);
    Task<ShowSoundContent?> OpenReadAsync(string blobPath);
    Task<IReadOnlyList<ShowBlobInfo>> ListAsync(string prefix);
    Task<bool> DeleteAsync(string blobPath);
}
