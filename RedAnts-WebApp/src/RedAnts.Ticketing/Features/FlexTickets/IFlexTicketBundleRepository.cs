using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.FlexTickets;

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

public interface IFlexTicketBundleRepository
{
    Task<FlexTicketBundle?> GetByIdAsync(int bundleId);
    Task SaveAsync(FlexTicketBundle bundle);

    Task<bool> ReferenceExistsAsync(int seasonId, string reference);

    Task<int> CreateAsync(int seasonId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null);

    Task<int> AddTicketsAsync(int bundleId, TicketCategory category, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null);

    Task<int> CreateEmptyAsync(int seasonId, TicketCategory category, string reference,
        string? createdByName = null, string? createdByEmail = null);

    Task<bool> DeleteEmptyAsync(int bundleId);

    Task<(int Created, int Updated)> ImportUnifiedAsync(int seasonId, IReadOnlyList<TicketImportRow> rows,
        string defaultBundle, TicketCategory defaultCategory,
        string? createdByName = null, string? createdByEmail = null);

    Task<Guid> CreateSingleAsync(int seasonId, TicketCategory category, string reference, CardHolder holder,
        string? createdByName = null, string? createdByEmail = null);

    Task SetHolderAsync(Guid uuid, CardHolder holder);

    Task SetTicketStatusAsync(Guid uuid, TicketStatus status);

    Task SetTicketRedeemedAsync(Guid uuid, bool redeemed);

    Task SetTicketCategoryAsync(Guid uuid, TicketCategory category);

    Task<FlexRebookResult> RebookByUuidAsync(int targetBundleId, Guid uuid, string? operatorName);

    Task<FlexRebookResult> RebookByCodeAsync(int targetBundleId, string codePrefix, string? operatorName);

    Task<FlexBoxOfficeResult> ConvertToBoxOfficeByUuidAsync(Guid uuid, string? operatorName);

    Task<FlexBoxOfficeResult> ConvertToBoxOfficeByCodeAsync(string codePrefix, string? operatorName);
}
