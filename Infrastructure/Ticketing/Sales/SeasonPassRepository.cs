using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class SeasonPassRepository(IScopeProvider scopeProvider) : ISeasonPasses
{
    public async Task<SeasonPass?> GetByUuidAsync(Guid uuid)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var row = await scope.Database.FirstOrDefaultAsync<SeasonPassRecord>(
            "WHERE Uuid = @0", uuid.ToString());
        return row is null ? null : Map(row);
    }

    public async Task<SeasonPass> SaveAsync(SeasonPass pass)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var row = new SeasonPassRecord
        {
            Id = pass.Id,
            Uuid = pass.Uuid.ToString(),
            SeasonId = pass.SeasonId,
            Category = (int)pass.Category,
            Price = pass.Price,
            OrderId = pass.OrderId,
            Status = (int)pass.Status,
            CreatedAt = pass.CreatedAt,
            BuyerType = (int?)pass.Buyer?.Type,
            BuyerFirstName = pass.Buyer?.FirstName,
            BuyerLastName = pass.Buyer?.LastName,
            BuyerCompany = pass.Buyer?.Company,
            CreatedByName = pass.CreatedByName
        };
        if (row.Id == 0) await scope.Database.InsertAsync(row);
        else await scope.Database.UpdateAsync(row);
        return Map(row);
    }

    private static SeasonPass Map(SeasonPassRecord r) =>
        SeasonPass.FromPersistence(
            r.Id,
            Guid.TryParse(r.Uuid, out var uuid) ? uuid : Guid.Empty,
            r.SeasonId,
            (TicketCategory)r.Category,
            r.Price,
            r.OrderId,
            (TicketStatus)r.Status,
            r.CreatedAt,
            Buyer.FromPersistence(r.BuyerType ?? 0, r.BuyerFirstName, r.BuyerLastName, r.BuyerCompany),
            r.CreatedByName);
}
