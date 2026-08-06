using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public sealed record MemberCardListItem(
    Guid Uuid,
    string? FirstName,
    string? LastName,
    DateOnly? Birthday,
    MemberCategory Category,
    TicketStatus Status,
    DateTime CreatedAt,
    int EventVisits,
    string? Reference,
    string? Email = null,
    string? CreatedByName = null,
    MemberAddress? Address = null,
    int Admissions = 1)
{
    public string HolderName => $"{FirstName} {LastName}".Trim();
    public bool HasName => !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName);
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
    public string CategoryLabel => Category.DisplayName();
    public bool IsCancelled => Status == TicketStatus.Cancelled;
}

public interface IMemberCardAdminReport
{
    Task<IReadOnlyList<MemberCardListItem>> GetBySeasonAsync(int seasonId);
}
