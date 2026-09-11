using System.Text.Json.Serialization;

namespace RedAnts.Show.Features.Remote;

[JsonConverter(typeof(JsonStringEnumConverter<ShowSlotKind>))]
public enum ShowSlotKind { Empty, Tile, Folder, Back, Pause, Fade }

public sealed record ShowBoardSlot(
    int Number,
    ShowSlotKind Kind,
    string? TileId,
    string Label,
    string? Icon,
    string? Color,
    bool Active,
    bool Enabled);

public sealed record ShowBoardView(
    string ProfileId,
    string ProfileName,
    string? ProfileColor,
    IReadOnlyList<string> Path,
    IReadOnlyList<ShowBoardSlot> Slots,
    string? NowPlaying,
    bool Paused,
    bool Unlocked)
{
    public bool SameAs(ShowBoardView other) =>
        ProfileId == other.ProfileId
        && ProfileName == other.ProfileName
        && ProfileColor == other.ProfileColor
        && NowPlaying == other.NowPlaying
        && Paused == other.Paused
        && Unlocked == other.Unlocked
        && Path.SequenceEqual(other.Path)
        && Slots.SequenceEqual(other.Slots);
}

public sealed record PublishedBoardView(long Version, ShowBoardView View);
