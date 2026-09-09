using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admission.Admin;

public sealed record FreeEntryListItem(
    Guid Uuid,
    DateTimeOffset CreatedAt,
    string? GrantedBy,
    bool IsInside,
    FreeEntryType? Category);
