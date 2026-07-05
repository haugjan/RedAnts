using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record FlexTicketBundleView(
    int Id,
    int SeasonId,
    TicketCategory Category,
    string Reference,
    DateTime CreatedAt,
    int TicketCount,
    int RedeemedCount,
    string? CreatedByName = null,
    string? CreatedByEmail = null);

public sealed record FlexTicketView(
    Guid Uuid,
    TicketStatus Status,
    bool Redeemed,
    int? RedeemedEventId,
    DateTime CreatedAt,
    TicketCategory Category = TicketCategory.Adult,
    bool? IsInside = null);

public interface IFlexTicketBundles
{
    Task<IReadOnlyList<FlexTicketBundleView>> GetBySeasonAsync(int seasonId);

    Task<IReadOnlyList<FlexTicketView>> GetTicketsAsync(int bundleId);

    Task SetTicketStatusAsync(Guid uuid, TicketStatus status);

    Task SetTicketRedeemedAsync(Guid uuid, bool redeemed);

    Task SetTicketCategoryAsync(Guid uuid, TicketCategory category);

    Task<bool> ReferenceExistsAsync(int seasonId, string reference);

    Task<FlexTicketBundleView> CreateAsync(int seasonId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null);
}
