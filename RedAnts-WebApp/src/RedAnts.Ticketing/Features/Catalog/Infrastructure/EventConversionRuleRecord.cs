// Uses Umbraco 17 APIs deprecated for removal in Umbraco 18 (content/data-type Save, DataType GetAll,
// FileService templates, Constants.Security.SuperUserId, IPublishedContent.Parent, SpecialDbTypes.NTEXT).
// Still functional; migrate to the async management services at the Umbraco 18 upgrade.
#pragma warning disable CS0618
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

[TableName("EventConversionRules")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventConversionRuleRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("EventId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int EventId { get; set; }
    [Column("CardType")] [NullSetting(NullSetting = NullSettings.NotNull)] public int CardType { get; set; }
    [Column("Discount")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Discount { get; set; }
}
