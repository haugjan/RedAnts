using System.Text.Json.Serialization;

namespace RedAnts.Features.Show;

public enum TileSize { Normal, Wide, Tall, Big }

public enum SoundKind { Local, Spotify }

public sealed record ShowSound(SoundKind Kind, string Ref, double StartSec = 0, double? DurationSec = null, bool Shuffle = false);

public sealed record ShowButton(
    string Id,
    string Label,
    string? Icon = null,
    string? Color = null,
    TileSize Size = TileSize.Normal,
    IReadOnlyList<ShowButton>? Children = null,
    ShowSound? Sound = null,
    string? Subtitle = null,
    IReadOnlyList<ShowSound>? Pool = null,
    int X = -1,
    int Y = -1,
    int W = 0,
    int H = 0,
    bool Panic = false)
{
    [JsonIgnore] public bool IsFolder => Children is { Count: > 0 };
    [JsonIgnore] public bool IsRandom => Pool is { Count: > 0 };
}

public sealed record ShowProfile(string Id, string Name, string? Color, IReadOnlyList<ShowButton> Root);
