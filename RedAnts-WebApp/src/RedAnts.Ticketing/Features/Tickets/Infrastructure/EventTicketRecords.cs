// Uses Umbraco 17 APIs deprecated for removal in Umbraco 18 (content/data-type Save, DataType GetAll,
// FileService templates, Constants.Security.SuperUserId, IPublishedContent.Parent, SpecialDbTypes.NTEXT).
// Still functional; migrate to the async management services at the Umbraco 18 upgrade.
#pragma warning disable CS0618
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace RedAnts.Ticketing.Features.Tickets.Infrastructure;

[TableName("EventTickets")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class EventTicketRecord
{
    [Column("Id")] [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)] public int Id { get; set; }
    [Column("Uuid")] [NullSetting(NullSetting = NullSettings.NotNull)] [Length(36)] [Index(IndexTypes.UniqueNonClustered)] public string Uuid { get; set; } = "";
    [Column("EventId")] [NullSetting(NullSetting = NullSettings.NotNull)] [Index(IndexTypes.NonClustered)] public int EventId { get; set; }
    [Column("Category")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Category { get; set; }
    [Column("TierId")] [NullSetting(NullSetting = NullSettings.Null)] [Index(IndexTypes.NonClustered)] public int? TierId { get; set; }
    [Column("Price")] [NullSetting(NullSetting = NullSettings.NotNull)] public decimal Price { get; set; }
    [Column("OrderId")] [NullSetting(NullSetting = NullSettings.Null)] public int? OrderId { get; set; }
    [Column("Status")] [NullSetting(NullSetting = NullSettings.NotNull)] public int Status { get; set; }
    [Column("CreatedAt")] [NullSetting(NullSetting = NullSettings.NotNull)] public DateTimeOffset CreatedAt { get; set; }
    [Column("CustomName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(120)] public string? CustomName { get; set; }
    [Column("Redeemed")] [NullSetting(NullSetting = NullSettings.NotNull)] public bool Redeemed { get; set; }
    [Column("BuyerType")] [NullSetting(NullSetting = NullSettings.Null)] public int? BuyerType { get; set; }
    [Column("BuyerFirstName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? BuyerFirstName { get; set; }
    [Column("BuyerLastName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? BuyerLastName { get; set; }
    [Column("BuyerCompany")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? BuyerCompany { get; set; }
    [Column("CreatedByName")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedByName { get; set; }
    [Column("CreatedByEmail")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? CreatedByEmail { get; set; }
    [Column("BundleId")] [NullSetting(NullSetting = NullSettings.Null)] [Index(IndexTypes.NonClustered)] public int? BundleId { get; set; }
    [Column("OriginType")] [NullSetting(NullSetting = NullSettings.Null)] public int? OriginType { get; set; }
    [Column("OriginCardUuid")] [NullSetting(NullSetting = NullSettings.Null)] [Length(36)] [Index(IndexTypes.NonClustered)] public string? OriginCardUuid { get; set; }
    [Column("Salutation")] [NullSetting(NullSetting = NullSettings.Null)] [Length(50)] public string? Salutation { get; set; }
    [Column("Birthday")] [NullSetting(NullSetting = NullSettings.Null)] public DateTime? Birthday { get; set; }
    [Column("Email")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? Email { get; set; }
    [Column("Street")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? Street { get; set; }
    [Column("AddressLine2")] [NullSetting(NullSetting = NullSettings.Null)] [Length(200)] public string? AddressLine2 { get; set; }
    [Column("PostalCode")] [NullSetting(NullSetting = NullSettings.Null)] [Length(20)] public string? PostalCode { get; set; }
    [Column("City")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? City { get; set; }
    [Column("Country")] [NullSetting(NullSetting = NullSettings.Null)] [Length(100)] public string? Country { get; set; }
    [Column("Phone")] [NullSetting(NullSetting = NullSettings.Null)] [Length(50)] public string? Phone { get; set; }
}
