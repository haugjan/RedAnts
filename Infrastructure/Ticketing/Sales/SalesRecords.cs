using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Infrastructure.Ticketing.Sales;

// Enum-typed columns (Category, Status, PaymentMethod, TicketType, FreeEntryType, VisitLogType) are
// stored as their integer value — the int column holds (int)enum; repositories cast back.

[TableName("Orders")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class OrderRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }

    [Column("OrderNumber")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(40)]
    [Index(IndexTypes.UniqueNonClustered)] public string OrderNumber { get; set; } = "";

    // Billing address (Rechnungsadresse) — kept on the financial record.
    [Column("BillingFirstName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingFirstName { get; set; } = "";
    [Column("BillingLastName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingLastName { get; set; } = "";
    [Column("BillingStreet")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string BillingStreet { get; set; } = "";
    [Column("BillingAddressLine2")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? BillingAddressLine2 { get; set; }
    [Column("BillingPostalCode")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(10)] public string BillingPostalCode { get; set; } = "";
    [Column("BillingCity")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingCity { get; set; } = "";
    [Column("BillingCountry")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingCountry { get; set; } = "Schweiz";
    [Column("BillingEmail")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string BillingEmail { get; set; } = "";
    [Column("BillingPhone")] [NullSetting(NullSetting = NullSettings.Null)] [Length(50)] public string? BillingPhone { get; set; }

    [Column("Currency")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(3)] public string Currency { get; set; } = "CHF";
    [Column("SubtotalNet")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal SubtotalNet { get; set; }
    [Column("VatRate")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal VatRate { get; set; }
    [Column("VatAmount")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal VatAmount { get; set; }
    [Column("TotalGross")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal TotalGross { get; set; }
    [Column("SellerUid")] [NullSetting(NullSetting = NullSettings.Null)] [Length(30)] public string? SellerUid { get; set; }

    [Column("PaymentMethod")] [NullSetting(NullSetting = NullSettings.NotNull)] public int PaymentMethod { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("PaidAt")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? PaidAt { get; set; }
}

[TableName("EventTickets")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventTicketRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("EventId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int EventId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("Redeemed")] [NullSetting(NullSetting = NullSettings.NotNull)] public bool Redeemed { get; set; }
}

[TableName("SeasonSingleTickets")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SeasonSingleTicketRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int SeasonId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("RedeemedEventId")] [NullSetting(NullSetting = NullSettings.Null)] public int? RedeemedEventId { get; set; }
    [Column("Redeemed")] [NullSetting(NullSetting = NullSettings.NotNull)] public bool Redeemed { get; set; }
    // Flextickets are issued in bundles; BundleId points to FlexTicketBundles (null for other origins).
    [Column("BundleId")] [NullSetting(NullSetting = NullSettings.Null)] [Index(IndexTypes.NonClustered)] public int? BundleId { get; set; }
}

[TableName("SeasonPasses")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SeasonPassRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int SeasonId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
}

[TableName("MembershipCards")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class MemberCardRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int SeasonId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("FirstName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? FirstName { get; set; }
    [Column("LastName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? LastName { get; set; }
    [Column("Birthday")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? Birthday { get; set; }
}

// One admission entitlement per (event, ticket). CheckedOutAt is gone; individual scans live in
// TicketEventVisitsLogs. TicketUuid is null for a FreeEntry visit (detail in TicketEventFreeEntries).
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
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
}

// Append-only in/out scans behind a visit's current presence.
[TableName("TicketEventVisitsLogs")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventVisitLogRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public long Id { get; set; }
    [Column("VisitId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public long VisitId { get; set; }
    [Column("Type")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Type { get; set; }
    [Column("OccurredAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime OccurredAt { get; set; }
    [Column("ScannedBy")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? ScannedBy { get; set; }
}

// Free-entry detail (which entitlement) for a FreeEntry visit — one row per free-entry visit.
[TableName("TicketEventFreeEntries")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventFreeEntryRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public long Id { get; set; }
    [Column("VisitId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.UniqueNonClustered)] public long VisitId { get; set; }
    [Column("FreeEntryType")] [NullSetting(NullSetting = NullSettings.NotNull)] public int FreeEntryType { get; set; }
}
