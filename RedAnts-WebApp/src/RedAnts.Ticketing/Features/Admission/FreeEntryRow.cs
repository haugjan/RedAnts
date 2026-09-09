using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admission;

public sealed record FreeEntryRow(
    Guid Uuid,
    DateTimeOffset CreatedAt,
    string? GrantedBy,
    bool IsInside,
    FreeEntryType? Category);
