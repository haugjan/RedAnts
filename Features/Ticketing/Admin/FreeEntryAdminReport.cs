using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public sealed record FreeEntryListItem(
    Guid Uuid,
    DateTime CreatedAt,
    string? GrantedBy,
    bool IsInside,
    FreeEntryType? Category);

public interface IFreeEntryAdminReport
{
    Task<IReadOnlyList<FreeEntryListItem>> GetByEventAsync(int eventId);
}

public interface IFreeEntryQuota
{
    Task<IReadOnlyDictionary<FreeEntryType, int?>> GetAsync(int eventId);

    Task SetAllAsync(int eventId, IReadOnlyDictionary<FreeEntryType, int?> quotas);
}
