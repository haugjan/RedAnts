using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class SeasonPassRepository(IScopeProvider scopeProvider, IPriceTiers priceTiers) : ISeasonPasses
{
    public async Task<(int Created, int Updated)> ImportUnifiedAsync(int seasonId, IReadOnlyList<TicketImportRow> rows,
        string defaultBundle, int? defaultTierId = null, string? createdByName = null, string? createdByEmail = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        var fallbackBundle = (defaultBundle ?? "").Trim();
        if (rows.Count == 0) return (0, 0);

        var tiers = await priceTiers.GetBySeasonAsync(seasonId);
        var mainTiers = tiers.Where(t => !t.IsPromo).ToList();
        var fallbackTierId = mainTiers.FirstOrDefault(t => t.Id == defaultTierId)?.Id ?? mainTiers.FirstOrDefault()?.Id;
        int? ResolveTier(string? category)
        {
            var c = (category ?? "").Trim();
            if (c.Length > 0)
            {
                var exact = mainTiers.FirstOrDefault(t => string.Equals(t.Name, c, StringComparison.OrdinalIgnoreCase));
                if (exact is not null) return exact.Id;
                var partial = mainTiers.FirstOrDefault(t =>
                    t.Name.Contains(c, StringComparison.OrdinalIgnoreCase)
                    || c.Contains(t.Name, StringComparison.OrdinalIgnoreCase));
                if (partial is not null) return partial.Id;
            }
            return fallbackTierId;
        }

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var created = 0;
        var updated = 0;

        foreach (var row in rows)
        {
            var bundle = string.IsNullOrWhiteSpace(row.Bundle) ? fallbackBundle : row.Bundle.Trim();
            var reference = bundle.Length == 0 ? null : bundle;
            var tierId = ResolveTier(row.Category);
            var h = row.Holder;
            var birthday = h.Birthday is { } b ? b.ToDateTime(TimeOnly.MinValue) : (DateTime?)null;
            var buyerType = (int)(h.IsCompany ? BuyerType.Company : BuyerType.Private);

            var code = (row.CardNo ?? "").Trim().ToLowerInvariant();
            if (code.Length == 8 && code.All(Uri.IsHexDigit))
            {
                var affected = await db.ExecuteAsync(
                    "UPDATE SeasonPasses SET TierId=@0, Reference=ISNULL(@1, Reference), BuyerType=@2, " +
                    "BuyerFirstName=@3, BuyerLastName=@4, BuyerCompany=@5, BuyerEmail=@6, Salutation=@7, Birthday=@8, " +
                    "Street=@9, AddressLine2=@10, PostalCode=@11, City=@12, Country=@13, Phone=@14 " +
                    "WHERE SeasonId=@15 AND Uuid LIKE @16",
                    (object[])new object?[]
                    {
                        tierId, reference, buyerType, h.FirstName, h.LastName, h.Company, h.Email,
                        h.Salutation, birthday, h.Street, h.AddressLine2, h.PostalCode, h.City, h.Country, h.Phone,
                        seasonId, code + "%"
                    });
                if (affected > 0) { updated++; continue; }
            }

            var uuid = await TicketCode.AllocateAsync(db, Guid.NewGuid());
            var record = new SeasonPassRecord
            {
                Uuid = uuid.ToString(),
                SeasonId = seasonId,
                Category = 0,
                TierId = tierId,
                Price = 0m,
                OrderId = null,
                Status = (int)TicketStatus.Valid,
                CreatedAt = DateTime.UtcNow,
                BuyerType = buyerType,
                BuyerFirstName = h.FirstName,
                BuyerLastName = h.LastName,
                BuyerCompany = h.Company,
                CreatedByName = createdByName,
                CreatedByEmail = createdByEmail,
                Reference = reference,
                BuyerEmail = h.Email,
                Salutation = h.Salutation,
                Birthday = birthday,
                Street = h.Street,
                AddressLine2 = h.AddressLine2,
                PostalCode = h.PostalCode,
                City = h.City,
                Country = h.Country,
                Phone = h.Phone
            };
            await db.InsertAsync(record);
            created++;
        }

        return (created, updated);
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
            Category = 0,
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
            Reference = pass.Reference,
            BuyerEmail = pass.Email
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
            r.TierId,
            r.Price,
            r.OrderId,
            (TicketStatus)r.Status,
            r.CreatedAt,
            Buyer.FromPersistence(r.BuyerType ?? 0, r.BuyerFirstName, r.BuyerLastName, r.BuyerCompany),
            r.CreatedByName,
            r.CreatedByEmail,
            r.Reference,
            r.BuyerEmail);
}
