using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Infrastructure.Ticketing.Sales;

[TableName("EventTicketBundles")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventTicketBundleRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("EventId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int EventId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }

    [Column("Reference")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(50)]
    [Index(IndexTypes.UniqueNonClustered, ForColumns = "EventId,Reference", Name = "IX_EventTicketBundles_Event_Reference")]
    public string Reference { get; set; } = "";

    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("CreatedByName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedByName { get; set; }
    [Column("CreatedByEmail")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedByEmail { get; set; }
}
