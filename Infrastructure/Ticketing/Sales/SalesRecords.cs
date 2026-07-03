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

    [Column("PaymentMethod")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(20)] public string PaymentMethod { get; set; } = "Payrexx";
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(20)] public string Status { get; set; } = "Draft";
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
    [Column("CategoryCode")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(50)] public string CategoryCode { get; set; } = "";
    [Column("CategoryName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string CategoryName { get; set; } = "";
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(20)] public string Status { get; set; } = "Valid";
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("CheckedInAt")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? CheckedInAt { get; set; }
    [Column("RedeemedAt")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? RedeemedAt { get; set; }
    [Column("RedeemedBy")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? RedeemedBy { get; set; }
}

[TableName("SeasonSingleTickets")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SeasonSingleTicketRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int SeasonId { get; set; }
    [Column("CategoryCode")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(50)] public string CategoryCode { get; set; } = "";
    [Column("CategoryName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string CategoryName { get; set; } = "";
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(20)] public string Status { get; set; } = "Valid";
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("RedeemedEventId")] [NullSetting(NullSetting = NullSettings.Null)] public int? RedeemedEventId { get; set; }
    [Column("CheckedInAt")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? CheckedInAt { get; set; }
    [Column("RedeemedAt")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? RedeemedAt { get; set; }
    [Column("RedeemedBy")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? RedeemedBy { get; set; }
}

[TableName("SeasonPasses")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SeasonPassRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int SeasonId { get; set; }
    [Column("CategoryCode")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(50)] public string CategoryCode { get; set; } = "";
    [Column("CategoryName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string CategoryName { get; set; } = "";
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(20)] public string Status { get; set; } = "Valid";
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("RedeemedBy")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? RedeemedBy { get; set; }
}

[TableName("MembershipCards")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class MemberCardRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("SeasonId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int SeasonId { get; set; }
    [Column("CategoryCode")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(50)] public string CategoryCode { get; set; } = "";
    [Column("CategoryName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string CategoryName { get; set; } = "";
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(20)] public string Status { get; set; } = "Valid";
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CreatedAt { get; set; }
    [Column("FirstName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? FirstName { get; set; }
    [Column("LastName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? LastName { get; set; }
    [Column("Birthday")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? Birthday { get; set; }
    [Column("RedeemedBy")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? RedeemedBy { get; set; }
}

[TableName("TicketEventVisits")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventVisitRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public long Id { get; set; }
    [Column("TicketType")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(20)] public string TicketType { get; set; } = "";
    [Column("TicketUuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.NonClustered)] public string TicketUuid { get; set; } = "";
    [Column("EventId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int EventId { get; set; }
    [Column("CheckedInAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTime CheckedInAt { get; set; }
    [Column("CheckedOutAt")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? CheckedOutAt { get; set; }
    [Column("ScannedBy")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? ScannedBy { get; set; }
}
