using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admin;

public sealed record FreeEntryListItem(
    Guid Uuid,
    DateTimeOffset CreatedAt,
    string? GrantedBy,
    bool IsInside,
    FreeEntryType? Category);

public interface IFreeEntryAdminReport
{
    Task<IReadOnlyList<FreeEntryListItem>> GetByEventAsync(int eventId);
}
