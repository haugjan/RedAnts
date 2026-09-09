using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admin;

namespace RedAnts.Ticketing.Features.MemberCards.Admin;

public sealed record MemberCardListItem(
    Guid Uuid,
    string? FirstName,
    string? LastName,
    DateOnly? Birthday,
    MemberCategory Category,
    TicketStatus Status,
    DateTimeOffset CreatedAt,
    int EventVisits,
    string? Reference,
    string? Email = null,
    string? CreatedByName = null,
    MemberAddress? Address = null,
    int Admissions = 1,
    int Conversions = 0)
{
    public string HolderName => $"{FirstName} {LastName}".Trim();
    public bool HasName => !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName);
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
    public string CategoryLabel => Category.DisplayName();
    public bool IsCancelled => Status == TicketStatus.Cancelled;
    public bool IsCompany => !string.IsNullOrWhiteSpace(Address?.Company);
    public string? BuyerDisplay => AdminName.Display(Address?.Company, FirstName, LastName);
    public string SortKey => AdminName.SortKey(Address?.Company, FirstName, LastName);
    public CardHolder Holder => CardHolder.Create(
        IsCompany ? BuyerType.Company : BuyerType.Private,
        Address?.Salutation, Address?.Company, FirstName, LastName, Birthday, Email,
        Address?.Street, Address?.AddressLine2, Address?.PostalCode, Address?.City, Address?.Country, Address?.Phone);
}
