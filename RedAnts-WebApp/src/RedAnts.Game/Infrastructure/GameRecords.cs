using NPoco;

namespace RedAnts.Game.Infrastructure;

[TableName("game.Players")]
[PrimaryKey("ExternalId", AutoIncrement = false)]
[ExplicitColumns]
public class GamePlayerRecord
{
    [Column("ExternalId")] public int ExternalId { get; set; }
    [Column("Name")] public string Name { get; set; } = "";
    [Column("Position")] public int Position { get; set; }
    [Column("Club")] public string Club { get; set; } = "";
    [Column("Number")] public string Number { get; set; } = "";
    [Column("BirthYear")] public int? BirthYear { get; set; }
    [Column("Height")] public string? Height { get; set; }
    [Column("PortraitUrl")] public string? PortraitUrl { get; set; }
    [Column("MarketValue")] public int MarketValue { get; set; }
    [Column("Games")] public int Games { get; set; }
    [Column("Goals")] public int Goals { get; set; }
    [Column("Assists")] public int Assists { get; set; }
    [Column("BestPlayer")] public int BestPlayer { get; set; }
    [Column("PenaltyMinutes")] public int PenaltyMinutes { get; set; }
    [Column("GoalsAgainstPerGame")] public decimal GoalsAgainstPerGame { get; set; }
    [Column("RawTotal")] public int RawTotal { get; set; }
    [Column("RawAverage")] public decimal RawAverage { get; set; }
    [Column("PeakPoints")] public int PeakPoints { get; set; }
    [Column("PeakGame")] public int PeakGame { get; set; }
    [Column("RecentAverage")] public decimal RecentAverage { get; set; }
    [Column("Status")] public int Status { get; set; }
    [Column("Licence")] public string? Licence { get; set; }
    [Column("FormCsv")] public string FormCsv { get; set; } = "";
    [Column("PointsJson")] public string? PointsJson { get; set; }
    [Column("Season")] public string Season { get; set; } = "";
    [Column("UpdatedAt")] public DateTimeOffset UpdatedAt { get; set; }
}

[TableName("game.Squads")]
[PrimaryKey("ManagerToken", AutoIncrement = false)]
[ExplicitColumns]
public class SquadRecord
{
    [Column("ManagerToken")] public string ManagerToken { get; set; } = "";
    [Column("Name")] public string Name { get; set; } = "";
    [Column("CreatedAt")] public DateTimeOffset CreatedAt { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset UpdatedAt { get; set; }
}

[TableName("game.SquadSlots")]
[PrimaryKey("ManagerToken,Slot", AutoIncrement = false)]
[ExplicitColumns]
public class SquadSlotRecord
{
    [Column("ManagerToken")] public string ManagerToken { get; set; } = "";
    [Column("Slot")] public int Slot { get; set; }
    [Column("PlayerId")] public int PlayerId { get; set; }
    [Column("MarketValue")] public int MarketValue { get; set; }
}

[TableName("game.Settings")]
[PrimaryKey("Key", AutoIncrement = false)]
[ExplicitColumns]
public class GameSettingRecord
{
    [Column("Key")] public string Key { get; set; } = "";
    [Column("Value")] public string? Value { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset UpdatedAt { get; set; }
}
