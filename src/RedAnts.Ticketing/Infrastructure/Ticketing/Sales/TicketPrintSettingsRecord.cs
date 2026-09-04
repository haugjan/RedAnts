using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Infrastructure.Ticketing.Sales;

[TableName("TicketPrintSettings")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class TicketPrintSettingsRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }

    [Column("TicketType")] [NullSetting(NullSetting = NullSettings.NotNull)]
    [Index(IndexTypes.UniqueNonClustered, Name = "IX_TicketPrintSettings_TicketType")]
    public int TicketType { get; set; }

    [Column("PageWMm")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal PageWMm { get; set; }
    [Column("PageHMm")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal PageHMm { get; set; }
    [Column("QrXMm")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal QrXMm { get; set; }
    [Column("QrYMm")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal QrYMm { get; set; }
    [Column("QrSizeMm")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal QrSizeMm { get; set; }
    [Column("CodeFontPt")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal CodeFontPt { get; set; }
    [Column("ShowName")] [NullSetting(NullSetting = NullSettings.NotNull)] public bool ShowName { get; set; }
    [Column("NameXMm")] [NullSetting(NullSetting = NullSettings.Null)] public decimal? NameXMm { get; set; }
    [Column("NameYMm")] [NullSetting(NullSetting = NullSettings.Null)] public decimal? NameYMm { get; set; }
    [Column("NameFontPt")] [NullSetting(NullSetting = NullSettings.Null)] public decimal? NameFontPt { get; set; }
    [Column("NameMaxWidthMm")] [NullSetting(NullSetting = NullSettings.Null)] public decimal? NameMaxWidthMm { get; set; }
    [Column("NameAlign")] [NullSetting(NullSetting = NullSettings.Null)] public int? NameAlign { get; set; }
}
