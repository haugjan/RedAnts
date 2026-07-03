using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Infrastructure.Ticketing.Sales;

// Flexticket bundle: a batch of season single-admission tickets issued together, identified by a
// reference that is unique within its season. Category is stored as (int)TicketCategory. The bundle's
// individual tickets live in SeasonSingleTickets and reference this row via BundleId.
[TableName("FlexTicketBundles")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class FlexTicketBundleRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int SeasonId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }

    // Unique per season (composite index on SeasonId + Reference).
    [Column("Reference")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(50)]
    [Index(IndexTypes.UniqueNonClustered, ForColumns = "SeasonId,Reference", Name = "IX_FlexTicketBundles_Season_Reference")]
    public string Reference { get; set; } = "";

    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
}
