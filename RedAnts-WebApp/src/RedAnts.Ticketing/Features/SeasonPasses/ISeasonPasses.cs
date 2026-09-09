using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.SeasonPasses;

public interface ISeasonPasses
{
    Task<SeasonPass?> GetByUuidAsync(Guid uuid);
    Task<IReadOnlyList<SeasonPass>> GetByOrderAsync(int orderId);
    Task<SeasonPass> SaveAsync(SeasonPass pass);
    Task SetHolderAsync(Guid uuid, CardHolder holder);
    Task<(int Created, int Updated)> ImportUnifiedAsync(int seasonId, IReadOnlyList<TicketImportRow> rows,
        string defaultBundle, int? defaultTierId = null, string? createdByName = null, string? createdByEmail = null);
}
