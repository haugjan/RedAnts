using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>One member card (Mitgliederkarte) in the admin list for a season, with the info an admin
/// cares about. <see cref="EventVisits"/> is the number of distinct events the card was admitted to.</summary>
public sealed record MemberCardListItem(
    Guid Uuid,
    string? FirstName,
    string? LastName,
    DateOnly? Birthday,
    TicketCategory Category,
    TicketStatus Status,
    DateTime CreatedAt,
    int EventVisits,
    string? Reference)
{
    public string HolderName => $"{FirstName} {LastName}".Trim();
    public bool HasName => !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName);
    public string CategoryLabel => Category.DisplayName();
    public bool IsCancelled => Status == TicketStatus.Cancelled;
}

/// <summary>Read side for the Mitgliederkarten admin tab. Self-contained: reads the member cards and
/// their visit counts straight from the Sales tables, independent of the ticket repositories.</summary>
public interface IMemberCardAdminReport
{
    Task<IReadOnlyList<MemberCardListItem>> GetBySeasonAsync(int seasonId);
}
