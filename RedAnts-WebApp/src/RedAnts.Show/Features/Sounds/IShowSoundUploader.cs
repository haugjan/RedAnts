namespace RedAnts.Show.Features.Sounds;

public sealed record ShowSoundContent(Stream Content, string ContentType, DateTimeOffset? LastModified, string? ETag);

public interface IShowSoundUploader
{
    Task<string> UploadAsync(string fileName, Stream content, string? contentType);
    Task UploadAtPathAsync(string blobPath, Stream content, string? contentType);
    Task<byte[]?> DownloadAsync(string blobPath);
    Task<ShowSoundContent?> OpenReadAsync(string blobPath);
}
