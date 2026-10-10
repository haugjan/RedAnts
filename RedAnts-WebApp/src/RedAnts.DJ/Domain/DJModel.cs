using System.Text.Json.Serialization;

namespace RedAnts.DJ.Domain;

public enum TileSize { Normal, Wide, Tall, Big }

public enum SoundKind { Local, Spotify }

public sealed record DJSound(
    SoundKind Kind,
    string Ref,
    double StartSec = 0,
    double? DurationSec = null,
    bool Shuffle = false,
    string? Title = null,
    string? Artist = null,
    string? CoverUrl = null);

public sealed record DJButton(
    string Id,
    string Label,
    string? Icon = null,
    string? Color = null,
    TileSize Size = TileSize.Normal,
    IReadOnlyList<DJButton>? Children = null,
    DJSound? Sound = null,
    string? Subtitle = null,
    IReadOnlyList<DJSound>? Pool = null,
    int X = -1,
    int Y = -1,
    int W = 0,
    int H = 0,
    bool Panic = false,
    IReadOnlyList<DJSound>? Songs = null,
    bool SongsRandom = false,
    DJControl? Control = null)
{
    [JsonIgnore] public bool IsFolder => Children is { Count: > 0 };
    [JsonIgnore] public bool IsRandom => Pool is { Count: > 0 };
    [JsonIgnore] public bool IsControl => Control is not null;

    [JsonIgnore]
    public IReadOnlyList<DJSound> EffectiveSongs =>
        Songs is { Count: > 0 } ? Songs
        : Sound is { } s ? new[] { s }
        : Pool ?? Array.Empty<DJSound>();
}

public sealed record DJProfile(string Id, string Name, string? Color, IReadOnlyList<DJButton> Root, int LayoutVersion = 1);
