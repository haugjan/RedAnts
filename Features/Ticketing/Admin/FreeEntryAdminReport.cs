namespace RedAnts.Features.Ticketing.Admin;

public sealed record FreeEntryListItem(
    Guid Uuid,
    DateTime CreatedAt,
    string? GrantedBy,
    bool IsInside);

public interface IFreeEntryAdminReport
{
    Task<IReadOnlyList<FreeEntryListItem>> GetByEventAsync(int eventId);
}
