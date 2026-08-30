using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RedAnts.Infrastructure.Show;

public interface IShowSoundUploader
{
    Task<string> UploadAsync(string fileName, Stream content, string? contentType);
}

public sealed partial class ShowSoundUploader(IOptions<ShowStorageOptions> options) : IShowSoundUploader
{
    public async Task<string> UploadAsync(string fileName, Stream content, string? contentType)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ConnectionString))
            throw new InvalidOperationException("Kein Show:Storage:ConnectionString konfiguriert. Datei-Upload ist nicht möglich.");

        var container = new BlobContainerClient(opts.ConnectionString, opts.Container);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var safe = Sanitize(Path.GetFileName(fileName));
        var stem = Path.GetFileNameWithoutExtension(safe);
        var ext = Path.GetExtension(safe);
        if (string.IsNullOrEmpty(ext)) ext = ".mp3";
        var blobPath = $"sounds/{stem}-{Guid.NewGuid().ToString("N")[..6]}{ext}";
        var blob = container.GetBlobClient(blobPath);

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        buffer.Position = 0;

        await blob.UploadAsync(buffer, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/mpeg" : contentType }
        });
        return blobPath;
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
