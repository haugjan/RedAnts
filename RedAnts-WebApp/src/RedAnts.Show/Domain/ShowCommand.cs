namespace RedAnts.Show.Domain;

public sealed record ShowCommand(string Action, string? TileId = null, string? ProfileId = null, int? SongIndex = null, string? Room = null);
