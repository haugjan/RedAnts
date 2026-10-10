namespace RedAnts.DJ.Features.Sounds;

public sealed record DJSoundContent(Stream Content, string ContentType, DateTimeOffset? LastModified, string? ETag);

public sealed record DJBlobInfo(string Path, long SizeBytes, DateTimeOffset? LastModified);

public interface IDJSoundUploader
{
    Task<string> UploadAsync(string fileName, Stream content, string? contentType);
    Task UploadAtPathAsync(string blobPath, Stream content, string? contentType);
    Task<byte[]?> DownloadAsync(string blobPath);
    Task<DJSoundContent?> OpenReadAsync(string blobPath);
    Task<IReadOnlyList<DJBlobInfo>> ListAsync(string prefix);
    Task<bool> DeleteAsync(string blobPath);
}
