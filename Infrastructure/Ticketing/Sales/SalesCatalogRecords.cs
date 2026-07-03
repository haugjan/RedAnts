using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Infrastructure.Ticketing.Sales;

// Pricing catalog: an event/season has 0..1 price row (unique on the node id), each with n category
// sub-rows (Category enum stored as int, SalePrice, Quota). Category has no code/name column anymore.

[TableName("EventPrices")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventPriceRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("EventId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.UniqueNonClustered)] public int EventId { get; set; }
    [Column("TotalSalesQuota")] [NullSetting(NullSetting = NullSettings.Null)] public int? TotalSalesQuota { get; set; }
    [Column("AdmissionQuota")] [NullSetting(NullSetting = NullSettings.Null)] public int? AdmissionQuota { get; set; }
}

[TableName("EventPriceCategories")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventPriceCategoryRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("EventPriceId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int EventPriceId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("SalePrice")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal SalePrice { get; set; }
    [Column("Quota")] [NullSetting(NullSetting = NullSettings.Null)] public int? Quota { get; set; }
}

[TableName("SeasonPrices")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SeasonPriceRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.UniqueNonClustered)] public int SeasonId { get; set; }
}

[TableName("SeasonPriceCategories")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SeasonPriceCategoryRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("SeasonPriceId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int SeasonPriceId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("SalePrice")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal SalePrice { get; set; }
    [Column("Quota")] [NullSetting(NullSetting = NullSettings.Null)] public int? Quota { get; set; }
}
