using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Infrastructure.Ticketing.Sales;

[TableName("Orders")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class OrderRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }

    [Column("OrderNumber")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(40)]
    [Index(IndexTypes.UniqueNonClustered)] public string OrderNumber { get; set; } = "";

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

    [Column("BillingType")] [NullSetting(NullSetting = NullSettings.Null)] public int? BillingType { get; set; }
    [Column("BillingCompany")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? BillingCompany { get; set; }

    [Column("PayrexxGatewayId")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? PayrexxGatewayId { get; set; }
    [Column("FulfillmentPayload")] [NullSetting(NullSetting = NullSettings.Null)] [SpecialDbType(SpecialDbTypes.NTEXT)] public string? FulfillmentPayload { get; set; }
}

[TableName("EventTickets")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventTicketRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("EventId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int EventId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("Redeemed")] [NullSetting(NullSetting = NullSettings.NotNull)] public bool Redeemed { get; set; }
    [Column("BuyerType")] [NullSetting(NullSetting = NullSettings.Null)] public int? BuyerType { get; set; }
    [Column("BuyerFirstName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? BuyerFirstName { get; set; }
    [Column("BuyerLastName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? BuyerLastName { get; set; }
    [Column("BuyerCompany")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? BuyerCompany { get; set; }
    [Column("CreatedByName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedByName { get; set; }
    [Column("CreatedByEmail")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedByEmail { get; set; }
    [Column("BundleId")] [NullSetting(NullSetting = NullSettings.Null)] [Index(IndexTypes.NonClustered)] public int? BundleId { get; set; }
}

[TableName("SeasonSingleTickets")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SeasonSingleTicketRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int SeasonId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("RedeemedEventId")] [NullSetting(NullSetting = NullSettings.Null)] public int? RedeemedEventId { get; set; }
    [Column("Redeemed")] [NullSetting(NullSetting = NullSettings.NotNull)] public bool Redeemed { get; set; }
    [Column("BundleId")] [NullSetting(NullSetting = NullSettings.Null)] [Index(IndexTypes.NonClustered)] public int? BundleId { get; set; }
}

[TableName("SeasonPasses")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SeasonPassRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int SeasonId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("BuyerType")] [NullSetting(NullSetting = NullSettings.Null)] public int? BuyerType { get; set; }
    [Column("BuyerFirstName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? BuyerFirstName { get; set; }
    [Column("BuyerLastName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? BuyerLastName { get; set; }
    [Column("BuyerCompany")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? BuyerCompany { get; set; }
    [Column("CreatedByName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedByName { get; set; }
    [Column("CreatedByEmail")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedByEmail { get; set; }
    [Column("Reference")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? Reference { get; set; }
}

[TableName("MembershipCards")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class MemberCardRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int SeasonId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("FirstName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? FirstName { get; set; }
    [Column("LastName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? LastName { get; set; }
    [Column("Birthday")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? Birthday { get; set; }
    [Column("Reference")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? Reference { get; set; }
    [Column("CreatedByName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedByName { get; set; }
    [Column("CreatedByEmail")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedByEmail { get; set; }
}

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
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.Null)] [Length(36)] public string? Uuid { get; set; }
}

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
}

[TableName("NewsletterSignups")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class NewsletterSignupRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Email")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] [Index(IndexTypes.NonClustered)] public string Email { get; set; } = "";
    [Column("Name")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? Name { get; set; }
    [Column("Source")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(50)] public string Source { get; set; } = "";
    [Column("SignedUpAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime SignedUpAt { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("TransferredAt")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? TransferredAt { get; set; }
}

[TableName("OrderStatusLogs")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class OrderStatusLogRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public long Id { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int OrderId { get; set; }
    [Column("ToStatus")] [NullSetting(NullSetting = NullSettings.NotNull)] public int ToStatus { get; set; }
    [Column("ChangedBy")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? ChangedBy { get; set; }
    [Column("OccurredAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime OccurredAt { get; set; }
    [Column("Note")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? Note { get; set; }
}

[TableName("OrderAddOns")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class OrderAddOnRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int OrderId { get; set; }
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int SeasonId { get; set; }
    [Column("SeasonName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string SeasonName { get; set; } = "";
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("CategoryName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string CategoryName { get; set; } = "";
    [Column("Label")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string Label { get; set; } = "";
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("Quantity")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Quantity { get; set; }
}
