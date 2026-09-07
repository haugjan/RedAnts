using System.Text.RegularExpressions;

namespace RedAnts.Domain.Show;

public sealed record SpotifyTrack(
    string Uri,
    string Name,
    string Artist,
    string Album = "",
    string CoverUrl = "",
    string? PreviewUrl = null,
    int DurationMs = 0);

public sealed record SpotifyContext(
    string Uri,
    string Kind,
    string Name,
    string Owner = "",
    string CoverUrl = "",
    int TrackCount = 0);

public static partial class ShowSpotifyLink
{
    public static (string Kind, string Id)? Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim();

        var uri = UriPattern().Match(s);
        if (uri.Success) return (uri.Groups[1].Value, uri.Groups[2].Value);

        var url = UrlPattern().Match(s);
        if (url.Success) return (url.Groups[1].Value, url.Groups[2].Value);

        if (BareId().IsMatch(s)) return ("track", s);
        return null;
    }

    public static string? ToUri(string? input)
    {
        var p = Parse(input);
        return p is { } v ? $"spotify:{v.Kind}:{v.Id}" : null;
    }

    [GeneratedRegex(@"^spotify:(track|playlist|album|artist|episode|show):([A-Za-z0-9]+)")]
    private static partial Regex UriPattern();

    [GeneratedRegex(@"open\.spotify\.com/(?:intl-[a-z-]+/)?(track|playlist|album|artist|episode|show)/([A-Za-z0-9]+)")]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"^[A-Za-z0-9]{22}$")]
    private static partial Regex BareId();
}
