using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public sealed record SeasonPassListItem(
    Guid Uuid,
    TicketCategory Category,
    string CategoryName,
    decimal Price,
    TicketStatus Status,
    DateTime CreatedAt,
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
    string? BuyerCompany = null)
{
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
    public bool IsCompany => !string.IsNullOrWhiteSpace(BuyerCompany);
    public string? BuyerDisplay => AdminName.Display(BuyerCompany, BuyerFirstName, BuyerLastName) ?? BuyerName;
    public string BuyerSortKey => AdminName.SortKey(BuyerCompany, BuyerFirstName, BuyerLastName) is { Length: > 0 } k ? k : (BuyerName ?? "");
}

public interface ISeasonPassAdminReport
{
    Task<IReadOnlyList<SeasonPassListItem>> GetBySeasonAsync(int seasonId);
}
