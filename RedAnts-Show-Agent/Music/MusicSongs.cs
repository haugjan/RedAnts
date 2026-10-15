using System.Text.Json;

namespace RedAnts.Show.Agent.Music;

public static class MusicSongs
{
    public const string Base = "/dj/sound/";

    public static IEnumerable<string> References(string? songsJson)
    {
        if (string.IsNullOrWhiteSpace(songsJson)) yield break;
        JsonDocument document;
        try { document = JsonDocument.Parse(songsJson); }
        catch (JsonException) { yield break; }
        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array) yield break;
            foreach (var song in document.RootElement.EnumerateArray())
            {
                if (song.ValueKind != JsonValueKind.Object) continue;
                if (song.TryGetProperty("t", out var kind) && kind.GetString() == "spotify") continue;
                if (!song.TryGetProperty("r", out var reference)) continue;
                if (reference.GetString() is not { Length: > 0 } url) continue;
                if (Reference(url) is { Length: > 0 } clean) yield return clean;
            }
        }
    }

    public static string Reference(string url)
    {
        var at = url.IndexOf(Base, StringComparison.OrdinalIgnoreCase);
        if (at >= 0) return Uri.UnescapeDataString(url[(at + Base.Length)..]);
        return url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? "" : Uri.UnescapeDataString(url.TrimStart('/'));
    }
}
