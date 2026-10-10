namespace RedAnts.DJ.Domain;

public sealed record DJCommand(string Action, string? TileId = null, string? ProfileId = null, int? SongIndex = null, string? Room = null, int? Slot = null);
