// Uses Umbraco 17 APIs deprecated for removal in Umbraco 18 (content/data-type Save, DataType GetAll,
// FileService templates, Constants.Security.SuperUserId, IPublishedContent.Parent, SpecialDbTypes.NTEXT).
// Still functional; migrate to the async management services at the Umbraco 18 upgrade.
#pragma warning disable CS0618
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Ticketing.Features.Orders.Infrastructure;

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
    [Column("PaymentSource")] [NullSetting(NullSetting = NullSettings.Null)] public int? PaymentSource { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTimeOffset CreatedAt { get; set; }
    [Column("PaidAt")] [NullSetting(NullSetting = NullSettings.Null)] public DateTimeOffset? PaidAt { get; set; }

    [Column("BillingType")] [NullSetting(NullSetting = NullSettings.Null)] public int? BillingType { get; set; }
    [Column("BillingCompany")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? BillingCompany { get; set; }

    [Column("PayrexxGatewayId")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? PayrexxGatewayId { get; set; }
    [Column("FulfillmentPayload")] [NullSetting(NullSetting = NullSettings.Null)] [SpecialDbType(SpecialDbTypes.NTEXT)] public string? FulfillmentPayload { get; set; }
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
    [Column("OccurredAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTimeOffset OccurredAt { get; set; }
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
    [Column("TierId")] [NullSetting(NullSetting = NullSettings.Null)] public int? TierId { get; set; }
    [Column("CategoryName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string CategoryName { get; set; } = "";
    [Column("Label")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string Label { get; set; } = "";
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("Quantity")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Quantity { get; set; }
    [Column("Delivered")] [NullSetting(NullSetting = NullSettings.NotNull)] public bool Delivered { get; set; }
}

[TableName("OrderRefunds")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class OrderRefundRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("RefundNumber")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(40)] [Index(IndexTypes.UniqueNonClustered)] public string RefundNumber { get; set; } = "";
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int OrderId { get; set; }
    [Column("Amount")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Amount { get; set; }
    [Column("VatRate")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal VatRate { get; set; }
    [Column("VatAmount")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal VatAmount { get; set; }
    [Column("Currency")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(3)] public string Currency { get; set; } = "CHF";
    [Column("Method")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Method { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("PayrexxRefundId")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? PayrexxRefundId { get; set; }
    [Column("Reference")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? Reference { get; set; }
    [Column("Reason")] [NullSetting(NullSetting = NullSettings.Null)] [Length(500)] public string? Reason { get; set; }
    [Column("CreatedBy")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedBy { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTimeOffset CreatedAt { get; set; }
}

[TableName("AccountingJournal")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class AccountingJournalRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public long Id { get; set; }
    [Column("EntryNumber")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.UniqueNonClustered)] public long EntryNumber { get; set; }
    [Column("EntryType")] [NullSetting(NullSetting = NullSettings.NotNull)] public int EntryType { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int OrderId { get; set; }
    [Column("RefundId")] [NullSetting(NullSetting = NullSettings.Null)] public int? RefundId { get; set; }
    [Column("Amount")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Amount { get; set; }
    [Column("VatRate")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal VatRate { get; set; }
    [Column("VatAmount")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal VatAmount { get; set; }
    [Column("Currency")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(3)] public string Currency { get; set; } = "CHF";
    [Column("Reference")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? Reference { get; set; }
    [Column("Description")] [NullSetting(NullSetting = NullSettings.Null)] [Length(500)] public string? Description { get; set; }
    [Column("CreatedBy")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedBy { get; set; }
    [Column("OccurredAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTimeOffset OccurredAt { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTimeOffset CreatedAt { get; set; }
}

[TableName("OrderItems")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class OrderItemRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int OrderId { get; set; }
    [Column("Kind")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Kind { get; set; }
    [Column("ArticleGuid")] [NullSetting(NullSetting = NullSettings.Null)] public Guid? ArticleGuid { get; set; }
    [Column("RefId")] [NullSetting(NullSetting = NullSettings.NotNull)] public int RefId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("Label")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string Label { get; set; } = "";
    [Column("Quantity")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Quantity { get; set; }
    [Column("UnitPrice")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal UnitPrice { get; set; }
}
