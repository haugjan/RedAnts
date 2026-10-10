using System.Text.Json.Serialization;

namespace RedAnts.DJ.Features.Remote;

[JsonConverter(typeof(JsonStringEnumConverter<DJSlotKind>))]
public enum DJSlotKind { Empty, Tile, Folder, Back, Pause, Fade, Previous, Next }

public sealed record DJBoardSlot(
    int Number,
    DJSlotKind Kind,
    string? TileId,
    string Label,
    string? Icon,
    string? Color,
    bool Active,
    bool Enabled);

public sealed record DJBoardView(
    string ProfileId,
    string ProfileName,
    string? ProfileColor,
    IReadOnlyList<string> Path,
    IReadOnlyList<DJBoardSlot> Slots,
    string? NowPlaying,
    bool Paused,
    bool Unlocked)
{
    public bool SameAs(DJBoardView other) =>
        ProfileId == other.ProfileId
        && ProfileName == other.ProfileName
        && ProfileColor == other.ProfileColor
        && NowPlaying == other.NowPlaying
        && Paused == other.Paused
        && Unlocked == other.Unlocked
        && Path.SequenceEqual(other.Path)
        && Slots.SequenceEqual(other.Slots);
}

public sealed record PublishedBoardView(long Version, DJBoardView View);
