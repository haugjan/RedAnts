using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Infrastructure.Ticketing.Sales;

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
    [Column("AvailableUntil")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? AvailableUntil { get; set; }
}

[TableName("SeasonPrices")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SeasonPriceRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.UniqueNonClustered)] public int SeasonId { get; set; }
    [Column("TotalSalesQuota")] [NullSetting(NullSetting = NullSettings.Null)] public int? TotalSalesQuota { get; set; }
}

[TableName("SeasonAddOns")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SeasonAddOnRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int SeasonId { get; set; }
    [Column("Label")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string Label { get; set; } = "";
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("Active")] [NullSetting(NullSetting = NullSettings.NotNull)] public bool Active { get; set; }
    [Column("SortOrder")] [NullSetting(NullSetting = NullSettings.NotNull)] public int SortOrder { get; set; }
    [Column("Scope")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Scope { get; set; }
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
    [Column("TicketPrice")] [NullSetting(NullSetting = NullSettings.Null)] public decimal? TicketPrice { get; set; }
    [Column("Offered")] [NullSetting(NullSetting = NullSettings.Null)] public bool? Offered { get; set; }
    [Column("TicketOffered")] [NullSetting(NullSetting = NullSettings.Null)] public bool? TicketOffered { get; set; }
    [Column("TicketQuota")] [NullSetting(NullSetting = NullSettings.Null)] public int? TicketQuota { get; set; }
    [Column("PassAvailableUntil")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? PassAvailableUntil { get; set; }
    [Column("TicketAvailableUntil")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? TicketAvailableUntil { get; set; }
}
