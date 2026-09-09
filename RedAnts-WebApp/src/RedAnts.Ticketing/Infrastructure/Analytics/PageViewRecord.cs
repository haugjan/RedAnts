using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Ticketing.Infrastructure.Analytics;

[TableName("PageViews")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class PageViewRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public long Id { get; set; }
    [Column("OccurredAt")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public DateTimeOffset OccurredAt { get; set; }
    [Column("Path")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(400)] public string Path { get; set; } = "";
    [Column("VisitorHash")] [NullSetting(NullSetting = NullSettings.Null)] [Length(64)] public string? VisitorHash { get; set; }
    [Column("IsBot")] [NullSetting(NullSetting = NullSettings.NotNull)] public bool IsBot { get; set; }
}
