extern alias AzureId;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RedAnts.Infrastructure.Show;

public interface IShowSoundUploader
{
    Task<string> UploadAsync(string fileName, Stream content, string? contentType);
    Task UploadAtPathAsync(string blobPath, Stream content, string? contentType);
    Task<byte[]?> DownloadAsync(string blobPath);
}

public sealed partial class ShowSoundUploader(IOptions<ShowStorageOptions> options) : IShowSoundUploader
{
    private BlobContainerClient Container()
    {
        var opts = options.Value;
        if (!string.IsNullOrWhiteSpace(opts.AccountUrl))
            return new BlobContainerClient(new Uri($"{opts.AccountUrl.TrimEnd('/')}/{opts.Container}"), new AzureId::Azure.Identity.DefaultAzureCredential());
        if (!string.IsNullOrWhiteSpace(opts.ConnectionString))
            return new BlobContainerClient(opts.ConnectionString, opts.Container);
        throw new InvalidOperationException("Weder Show:Storage:AccountUrl noch Show:Storage:ConnectionString konfiguriert. Datei-Upload ist nicht möglich.");
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string? contentType)
    {
        var safe = Sanitize(Path.GetFileName(fileName));
        var stem = Path.GetFileNameWithoutExtension(safe);
        var ext = Path.GetExtension(safe);
        if (string.IsNullOrEmpty(ext)) ext = ".mp3";
        var blobPath = $"sounds/{stem}-{Guid.NewGuid().ToString("N")[..6]}{ext}";
        await UploadAtPathAsync(blobPath, content, contentType);
        return blobPath;
    }

    public async Task UploadAtPathAsync(string blobPath, Stream content, string? contentType)
    {
        var container = Container();
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);
        var blob = container.GetBlobClient(blobPath);

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        buffer.Position = 0;

        await blob.UploadAsync(buffer, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/mpeg" : contentType }
        });
    }

    public async Task<byte[]?> DownloadAsync(string blobPath)
    {
        var blob = Container().GetBlobClient(blobPath);
        if (!await blob.ExistsAsync()) return null;
        using var ms = new MemoryStream();
        await blob.DownloadToAsync(ms);
        return ms.ToArray();
    }

    private static string Sanitize(string name)
    {
        var cleaned = Whitespace().Replace(name.Trim(), "-");
        cleaned = Invalid().Replace(cleaned, "");
        return string.IsNullOrWhiteSpace(cleaned) ? $"sound-{Guid.NewGuid():N}.mp3" : cleaned;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"[^A-Za-z0-9._-]")]
    private static partial Regex Invalid();
}
