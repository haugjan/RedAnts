using NPoco;

namespace RedAnts.Show.Infrastructure;

[TableName("show.Profiles")]
[PrimaryKey("Id", AutoIncrement = false)]
[ExplicitColumns]
public class ShowProfileRecord
{
    [Column("Id")] public string Id { get; set; } = "";
    [Column("SortOrder")] public int SortOrder { get; set; }
    [Column("Json")] public string Json { get; set; } = "";
    [Column("UpdatedAt")] public DateTime UpdatedAt { get; set; }
}

[TableName("show.Settings")]
[PrimaryKey("Key", AutoIncrement = false)]
[ExplicitColumns]
public class ShowSettingRecord
{
    [Column("Key")] public string Key { get; set; } = "";
    [Column("Value")] public string? Value { get; set; }
    [Column("UpdatedAt")] public DateTime UpdatedAt { get; set; }
}
