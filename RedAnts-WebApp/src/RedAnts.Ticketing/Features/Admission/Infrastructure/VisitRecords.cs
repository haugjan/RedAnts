// Uses Umbraco 17 APIs deprecated for removal in Umbraco 18 (content/data-type Save, DataType GetAll,
// FileService templates, Constants.Security.SuperUserId, IPublishedContent.Parent, SpecialDbTypes.NTEXT).
// Still functional; migrate to the async management services at the Umbraco 18 upgrade.
#pragma warning disable CS0618
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Ticketing.Features.Admission.Infrastructure;

[TableName("TicketEventVisits")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventVisitRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public long Id { get; set; }
    [Column("EventId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int EventId { get; set; }
    [Column("TicketType")] [NullSetting(NullSetting = NullSettings.NotNull)] public int TicketType { get; set; }
    [Column("TicketUuid")] [NullSetting(NullSetting = NullSettings.Null)] [Length(36)] [Index(IndexTypes.NonClustered)] public string? TicketUuid { get; set; }
    [Column("IsInside")] [NullSetting(NullSetting = NullSettings.NotNull)] public bool IsInside { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTimeOffset CreatedAt { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.Null)] [Length(36)] public string? Uuid { get; set; }
    [Column("OriginType")] [NullSetting(NullSetting = NullSettings.Null)] public int? OriginType { get; set; }
    [Column("OriginCardUuid")] [NullSetting(NullSetting = NullSettings.Null)] [Length(36)] [Index(IndexTypes.NonClustered)] public string? OriginCardUuid { get; set; }
}

[TableName("TicketEventVisitsLogs")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventVisitLogRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public long Id { get; set; }
    [Column("VisitId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public long VisitId { get; set; }
    [Column("Type")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Type { get; set; }
    [Column("OccurredAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTimeOffset OccurredAt { get; set; }
    [Column("ScannedBy")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? ScannedBy { get; set; }
}

[TableName("TicketEventFreeEntries")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventFreeEntryRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public long Id { get; set; }
    [Column("VisitId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.UniqueNonClustered)] public long VisitId { get; set; }
    [Column("FreeEntryType")] [NullSetting(NullSetting = NullSettings.NotNull)] public int FreeEntryType { get; set; }
}

[TableName("TicketEventFreeEntryQuotas")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventFreeEntryQuotaRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("EventId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.UniqueNonClustered)] public int EventId { get; set; }
    [Column("SuQuota")] [NullSetting(NullSetting = NullSettings.Null)] public int? SuQuota { get; set; }
    [Column("PlayerQuota")] [NullSetting(NullSetting = NullSettings.Null)] public int? PlayerQuota { get; set; }
    [Column("StaffQuota")] [NullSetting(NullSetting = NullSettings.Null)] public int? StaffQuota { get; set; }
    [Column("OfficialQuota")] [NullSetting(NullSetting = NullSettings.Null)] public int? OfficialQuota { get; set; }
    [Column("ChildQuota")] [NullSetting(NullSetting = NullSettings.Null)] public int? ChildQuota { get; set; }
    [Column("HelperQuota")] [NullSetting(NullSetting = NullSettings.Null)] public int? HelperQuota { get; set; }
    [Column("SuFixed")] [NullSetting(NullSetting = NullSettings.Null)] public int? SuFixed { get; set; }
    [Column("PlayerFixed")] [NullSetting(NullSetting = NullSettings.Null)] public int? PlayerFixed { get; set; }
    [Column("StaffFixed")] [NullSetting(NullSetting = NullSettings.Null)] public int? StaffFixed { get; set; }
    [Column("OfficialFixed")] [NullSetting(NullSetting = NullSettings.Null)] public int? OfficialFixed { get; set; }
    [Column("ChildFixed")] [NullSetting(NullSetting = NullSettings.Null)] public int? ChildFixed { get; set; }
    [Column("HelperFixed")] [NullSetting(NullSetting = NullSettings.Null)] public int? HelperFixed { get; set; }
}
