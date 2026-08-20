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
    bool? IsInside = null,
    bool ConvertedForPurchase = false,
    bool BoxOffice = false);

public enum FlexRebookStatus { Moved, AlreadyInTarget, WrongSeason, NotFound, AlreadyRedeemed }

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

public enum FlexBoxOfficeStatus { Converted, AlreadyBoxOffice, AlreadyRedeemed, NotFound }

public sealed record FlexBoxOfficeResult(
    FlexBoxOfficeStatus Status,
    string? Reference = null,
    string? Category = null)
{
    public bool Ok => Status is FlexBoxOfficeStatus.Converted or FlexBoxOfficeStatus.AlreadyBoxOffice;
}

public interface IFlexTicketBundles
{
    Task<IReadOnlyList<FlexTicketBundleView>> GetBySeasonAsync(int seasonId);

    Task<FlexRebookResult> RebookByUuidAsync(int targetBundleId, Guid uuid, string? operatorName);

    Task<FlexRebookResult> RebookByCodeAsync(int targetBundleId, string codePrefix, string? operatorName);

    Task<FlexBoxOfficeResult> ConvertToBoxOfficeByUuidAsync(Guid uuid, string? operatorName);

    Task<FlexBoxOfficeResult> ConvertToBoxOfficeByCodeAsync(string codePrefix, string? operatorName);

    Task<IReadOnlyList<FlexTicketView>> GetTicketsAsync(int bundleId);

    Task SetTicketStatusAsync(Guid uuid, TicketStatus status);

    Task SetTicketRedeemedAsync(Guid uuid, bool redeemed);

    Task SetTicketCategoryAsync(Guid uuid, TicketCategory category);

    Task<bool> ReferenceExistsAsync(int seasonId, string reference);

    Task<FlexTicketBundleView> CreateAsync(int seasonId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null);

    Task<FlexTicketBundleView> AddTicketsAsync(int bundleId, TicketCategory category, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null);

    Task<FlexTicketBundleView> CreateEmptyAsync(int seasonId, TicketCategory category, string reference,
        string? createdByName = null, string? createdByEmail = null);

    Task<bool> DeleteEmptyAsync(int bundleId);
}
