using NPoco;

namespace RedAnts.Show.Infrastructure;

[TableName("show.Halls")]
[PrimaryKey("Id", AutoIncrement = false)]
[ExplicitColumns]
public class HallRecord
{
    [Column("Id")] public string Id { get; set; } = "";
    [Column("Json")] public string Json { get; set; } = "";
    [Column("UpdatedAt")] public DateTime UpdatedAt { get; set; }
}

[TableName("show.Games")]
[PrimaryKey("Id", AutoIncrement = false)]
[ExplicitColumns]
public class GameRecord
{
    [Column("Id")] public string Id { get; set; } = "";
    [Column("Json")] public string Json { get; set; } = "";
    [Column("UpdatedAt")] public DateTime UpdatedAt { get; set; }
}

[TableName("show.Scenes")]
[PrimaryKey("Id", AutoIncrement = false)]
[ExplicitColumns]
public class SceneRecord
{
    [Column("Id")] public string Id { get; set; } = "";
    [Column("SortOrder")] public int SortOrder { get; set; }
    [Column("Json")] public string Json { get; set; } = "";
    [Column("UpdatedAt")] public DateTime UpdatedAt { get; set; }
}

[TableName("show.AgentReports")]
[PrimaryKey("HallId,Kind", AutoIncrement = false)]
[ExplicitColumns]
public class AgentReportRecord
{
    [Column("HallId")] public string HallId { get; set; } = "";
    [Column("Kind")] public string Kind { get; set; } = "";
    [Column("Json")] public string Json { get; set; } = "";
    [Column("At")] public DateTimeOffset At { get; set; }
}
