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

public enum FlexRebookStatus { Moved, AlreadyInTarget, WrongSeason, NotFound }

public sealed record FlexRebookResult(
    FlexRebookStatus Status,
    string? Reference = null,
    string? Category = null,
    string? FromBundle = null,
    string? ToBundle = null,
    int? TicketSeasonId = null)
{
    public bool Ok => Status is FlexRebookStatus.Moved or FlexRebookStatus.AlreadyInTarget;
}

public interface IFlexTicketBundles
{
    Task<IReadOnlyList<FlexTicketBundleView>> GetBySeasonAsync(int seasonId);

    Task<FlexRebookResult> RebookByUuidAsync(int targetBundleId, Guid uuid, string? operatorName);

    Task<FlexRebookResult> RebookByCodeAsync(int targetBundleId, string codePrefix, string? operatorName);

    Task<IReadOnlyList<FlexTicketView>> GetTicketsAsync(int bundleId);

    Task SetTicketStatusAsync(Guid uuid, TicketStatus status);

    Task SetTicketRedeemedAsync(Guid uuid, bool redeemed);

    Task SetTicketCategoryAsync(Guid uuid, TicketCategory category);

    Task<bool> ReferenceExistsAsync(int seasonId, string reference);

    Task<FlexTicketBundleView> CreateAsync(int seasonId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null);
}
