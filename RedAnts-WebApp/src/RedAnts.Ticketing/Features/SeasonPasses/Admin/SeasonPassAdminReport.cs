using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admin;

namespace RedAnts.Ticketing.Features.SeasonPasses.Admin;

public sealed record SeasonPassListItem(
    Guid Uuid,
    string CategoryName,
    decimal Price,
    TicketStatus Status,
    DateTimeOffset CreatedAt,
    int EventVisits,
    string? BuyerName,
    string? OrderNumber,
    string? PaymentState,
    BuyerType? BuyerType = null,
    string? CreatedByName = null,
    string? Reference = null,
    string? Email = null,
    string? BuyerFirstName = null,
    string? BuyerLastName = null,
    string? BuyerCompany = null,
    int Conversions = 0,
    int? TierId = null,
    CardHolder? Holder = null)
{
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
    public bool IsCompany => !string.IsNullOrWhiteSpace(BuyerCompany);
    public string? BuyerDisplay => AdminName.Display(BuyerCompany, BuyerFirstName, BuyerLastName) ?? BuyerName;
    public string BuyerSortKey => AdminName.SortKey(BuyerCompany, BuyerFirstName, BuyerLastName) is { Length: > 0 } k ? k : (BuyerName ?? "");
}
