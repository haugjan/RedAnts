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
    string? CreatedByName = null)
{
    public string HolderName => $"{FirstName} {LastName}".Trim();
    public bool HasName => !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName);
    public string CategoryLabel => Category.DisplayName();
    public bool IsCancelled => Status == TicketStatus.Cancelled;
}

public interface IMemberCardAdminReport
{
    Task<IReadOnlyList<MemberCardListItem>> GetBySeasonAsync(int seasonId);
}
