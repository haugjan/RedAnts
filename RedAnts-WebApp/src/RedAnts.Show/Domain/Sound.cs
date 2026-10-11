namespace RedAnts.Show.Domain;

public enum SoundSource { Blob, Spotify }

public sealed record SoundTrack(
    SoundSource Source,
    string Ref,
    string Title = "",
    string Artist = "",
    double StartSec = 0,
    double? DurationSec = null)
{
    public string Display => Title.Length > 0 ? (Artist.Length > 0 ? $"{Title} · {Artist}" : Title) : Ref;

    public bool IsSpotify => Source == SoundSource.Spotify;

    public SoundTrack Validated()
    {
        ValidationException.ThrowIfBlank(Ref, nameof(Ref), "Datei oder Spotify-Link");
        if (StartSec < 0) throw new ValidationException(nameof(StartSec), "Cue-Start darf nicht negativ sein.");
        if (DurationSec is <= 0) throw new ValidationException(nameof(DurationSec), "Cue-Länge muss grösser als null sein.");
        return this with { Ref = Ref.Trim(), Title = Title.Trim(), Artist = Artist.Trim() };
    }
}

public sealed record SoundCommand(
    string Id,
    string Label,
    string Color,
    string? Icon,
    IReadOnlyList<SoundTrack> Tracks,
    bool Random = false,
    bool Loop = false,
    int Volume = 100,
    double FadeOutSec = 0)
{
    public const string DefaultColor = "#2b3038";

    public string Target => "sound:" + Id;

    public bool IsPlaylist => Tracks.Count > 1;

    public static SoundCommand Create(string label) =>
        new(Slug.Of(label, "sound"), label.Trim(), DefaultColor, null, [], Volume: 100);

    public SoundCommand Validated()
    {
        ValidationException.ThrowIfBlank(Id, nameof(Id), "Kennung");
        ValidationException.ThrowIfBlank(Label, nameof(Label), "Bezeichnung");
        if (Volume is < 0 or > 100) throw new ValidationException(nameof(Volume), "Lautstärke: 0 bis 100 Prozent.");
        if (FadeOutSec < 0) throw new ValidationException(nameof(FadeOutSec), "Fade-out darf nicht negativ sein.");
        return this with { Id = Id.Trim(), Label = Label.Trim(), Tracks = Tracks.Select(t => t.Validated()).ToList() };
    }
}

public static class Slug
{
    public static string Of(string text, string fallback)
    {
        var slug = new string(text.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray())
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue");
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        return slug.Length > 0 ? slug : fallback + "-" + Guid.NewGuid().ToString("n")[..6];
    }
}
