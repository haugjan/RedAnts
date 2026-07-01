using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Infrastructure.Ticketing;

[TableName("SingleTickets")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SingleTicketRecord
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public int Id { get; set; }

    [Column("EventId")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public int EventId { get; set; }

    [Column("PriceCategory")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(20)]
    public string PriceCategory { get; set; } = "";

    [Column("Price")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public decimal Price { get; set; }

    [Column("PurchasedAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime PurchasedAt { get; set; }

    [Column("UsedAt")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public DateTime? UsedAt { get; set; }

    [Column("CheckedInAt")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public DateTime? CheckedInAt { get; set; }

    [Column("TicketId")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(36)]
    [Index(IndexTypes.UniqueNonClustered)]
    public string TicketId { get; set; } = "";

    // Billing address (Rechnungsadresse)
    [Column("BillingFirstName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingFirstName { get; set; } = "";
    [Column("BillingLastName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingLastName { get; set; } = "";
    [Column("BillingStreet")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string BillingStreet { get; set; } = "";
    [Column("BillingAddressLine2")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? BillingAddressLine2 { get; set; }
    [Column("BillingPostalCode")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(10)] public string BillingPostalCode { get; set; } = "";
    [Column("BillingCity")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingCity { get; set; } = "";
    [Column("BillingCountry")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingCountry { get; set; } = "Schweiz";
    [Column("BillingEmail")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string BillingEmail { get; set; } = "";
    [Column("BillingPhone")] [NullSetting(NullSetting = NullSettings.Null)] [Length(50)] public string? BillingPhone { get; set; }

    [Column("PaymentMethod")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(20)]
    public string PaymentMethod { get; set; } = "Payrexx";

    [Column("PayStatus")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(20)]
    public string PayStatus { get; set; } = "Pending";
}

[TableName("SeasonTickets")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class SeasonTicketRecord
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public int Id { get; set; }

    [Column("SeasonId")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public int SeasonId { get; set; }

    [Column("Category")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(20)]
    public string Category { get; set; } = "";

    [Column("AgeGroup")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(20)]
    public string AgeGroup { get; set; } = "";

    [Column("Price")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public decimal Price { get; set; }

    [Column("PurchasedAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime PurchasedAt { get; set; }

    [Column("SeasonTicketId")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(36)]
    [Index(IndexTypes.UniqueNonClustered)]
    public string SeasonTicketId { get; set; } = "";

    [Column("BillingFirstName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingFirstName { get; set; } = "";
    [Column("BillingLastName")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingLastName { get; set; } = "";
    [Column("BillingStreet")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string BillingStreet { get; set; } = "";
    [Column("BillingAddressLine2")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? BillingAddressLine2 { get; set; }
    [Column("BillingPostalCode")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(10)] public string BillingPostalCode { get; set; } = "";
    [Column("BillingCity")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingCity { get; set; } = "";
    [Column("BillingCountry")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(100)] public string BillingCountry { get; set; } = "Schweiz";
    [Column("BillingEmail")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(200)] public string BillingEmail { get; set; } = "";
    [Column("BillingPhone")] [NullSetting(NullSetting = NullSettings.Null)] [Length(50)] public string? BillingPhone { get; set; }

    [Column("PaymentMethod")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(20)]
    public string PaymentMethod { get; set; } = "Payrexx";

    [Column("PayStatus")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(20)]
    public string PayStatus { get; set; } = "Pending";

    [Column("RemainingAdmissions")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public int? RemainingAdmissions { get; set; }

    [Column("CheckedInAt")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public DateTime? CheckedInAt { get; set; }
}

[TableName("MemberCards")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class MemberCardRecord
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public int Id { get; set; }

    [Column("FirstName")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(100)]
    public string FirstName { get; set; } = "";

    [Column("LastName")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(100)]
    public string LastName { get; set; } = "";

    [Column("Birthday")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime Birthday { get; set; }

    [Column("SeasonId")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public int SeasonId { get; set; }
}

[TableName("TicketScanLog")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class TicketScanLogRecord
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public long Id { get; set; }

    [Column("TicketKind")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(20)]
    public string TicketKind { get; set; } = "";

    [Column("TicketRef")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(36)]
    public string TicketRef { get; set; } = "";

    [Column("Direction")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(10)]
    public string Direction { get; set; } = "";

    [Column("ScannedAt")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    public DateTime ScannedAt { get; set; }

    [Column("ScannedBy")]
    [NullSetting(NullSetting = NullSettings.NotNull)]
    [Length(200)]
    public string ScannedBy { get; set; } = "";
}
