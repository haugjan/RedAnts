using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;
using PaymentMethod = RedAnts.Domain.Ticketing.Sales.PaymentMethod;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class SeasonPassRepository(IScopeProvider scopeProvider, IOrders orders) : ISeasonPasses
{
    public async Task<int> ImportAsync(int seasonId, IReadOnlyList<SeasonPassImportRow> rows,
        string? createdByName = null, string? createdByEmail = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");

        var created = 0;
        foreach (var row in rows)
        {
            int? orderId = null;
            if (row.Address.IsComplete)
            {
                try
                {
                    var billing = BillingAddress.Create(
                        row.Buyer.Type, row.Buyer.FirstName, row.Buyer.LastName, row.Buyer.Company,
                        row.Address.Street!, null, row.Address.PostalCode ?? "", row.Address.City!,
                        row.Address.Country, row.Address.Email!, row.Address.Phone);
                    var number = await orders.NextOrderNumberAsync();
                    var order = Order.Create(number, billing, 0m, 0m, PaymentMethod.Invoice, sellerUid: null);
                    order.MarkPaid();
                    orderId = (await orders.SaveAsync(order)).Id;
                }
                catch (DomainException) { orderId = null; }
            }

            var pass = SeasonPass.Create(seasonId, row.Category, 0m, orderId, row.Buyer,
                createdByName, createdByEmail, row.Reference);
            await SaveAsync(pass);
            created++;
        }
        return created;
    }

    public async Task<SeasonPass?> GetByUuidAsync(Guid uuid)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var row = await scope.Database.FirstOrDefaultAsync<SeasonPassRecord>(
            "WHERE Uuid = @0", uuid.ToString());
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<SeasonPass>> GetByOrderAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<SeasonPassRecord>(
            "WHERE OrderId = @0 ORDER BY CreatedAt, Id", orderId);
        return rows.Select(Map).ToList();
    }

    public async Task<SeasonPass> SaveAsync(SeasonPass pass)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var uuid = pass.Id == 0 ? await TicketCode.AllocateAsync(scope.Database, pass.Uuid) : pass.Uuid;
        var row = new SeasonPassRecord
        {
            Id = pass.Id,
            Uuid = uuid.ToString(),
            SeasonId = pass.SeasonId,
            Category = (int)pass.Category,
            TierId = pass.TierId,
            Price = pass.Price,
            OrderId = pass.OrderId,
            Status = (int)pass.Status,
            CreatedAt = pass.CreatedAt,
            BuyerType = (int?)pass.Buyer?.Type,
            BuyerFirstName = pass.Buyer?.FirstName,
            BuyerLastName = pass.Buyer?.LastName,
            BuyerCompany = pass.Buyer?.Company,
            CreatedByName = pass.CreatedByName,
            CreatedByEmail = pass.CreatedByEmail,
            Reference = pass.Reference
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
            r.CreatedByName,
            r.CreatedByEmail,
            r.Reference,
            r.TierId);
}
