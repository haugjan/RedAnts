// Uses Umbraco 17 APIs deprecated for removal in Umbraco 18 (content/data-type Save, DataType GetAll,
// FileService templates, Constants.Security.SuperUserId, IPublishedContent.Parent, SpecialDbTypes.NTEXT).
// Still functional; migrate to the async management services at the Umbraco 18 upgrade.
#pragma warning disable CS0618
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Ticketing.Features.Newsletter.Infrastructure;

[TableName("NewsletterSignups")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class NewsletterSignupRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Email")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] [Index(IndexTypes.NonClustered)] public string Email { get; set; } = "";
    [Column("Name")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? Name { get; set; }
    [Column("Source")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(50)] public string Source { get; set; } = "";
    [Column("SignedUpAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTimeOffset SignedUpAt { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("TransferredAt")] [NullSetting(NullSetting = NullSettings.Null)] public DateTimeOffset? TransferredAt { get; set; }
}
