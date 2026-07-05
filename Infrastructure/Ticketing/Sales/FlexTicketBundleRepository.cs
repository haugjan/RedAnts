using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class FlexTicketBundleRepository(IScopeProvider scopeProvider) : IFlexTicketBundles
{
    public const int MaxBundleSize = 1000;

    public async Task<IReadOnlyList<FlexTicketBundleView>> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var bundles = await scope.Database.FetchAsync<FlexTicketBundleRecord>(
            "WHERE SeasonId = @0 ORDER BY CreatedAt DESC", seasonId);
        if (bundles.Count == 0) return [];

        var counts = await scope.Database.FetchAsync<BundleCountRow>(
            "SELECT BundleId AS BundleId, COUNT(*) AS Total, " +
            "SUM(CASE WHEN Redeemed = 1 THEN 1 ELSE 0 END) AS Redeemed " +
            "FROM SeasonSingleTickets WHERE SeasonId = @0 AND BundleId IS NOT NULL GROUP BY BundleId",
            seasonId);
        var byBundle = counts.ToDictionary(c => c.BundleId, c => c);

        return bundles.Select(b =>
        {
            var c = byBundle.GetValueOrDefault(b.Id);
            return new FlexTicketBundleView(b.Id, b.SeasonId, (TicketCategory)b.Category, b.Reference,
                b.CreatedAt, c?.Total ?? 0, c?.Redeemed ?? 0, b.CreatedByName, b.CreatedByEmail);
        }).ToList();
    }

    public async Task<IReadOnlyList<FlexTicketView>> GetTicketsAsync(int bundleId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<SeasonSingleTicketRecord>(
            "WHERE BundleId = @0 ORDER BY CreatedAt", bundleId);
        return rows.Select(r => new FlexTicketView(
            Guid.TryParse(r.Uuid, out var uuid) ? uuid : Guid.Empty,
            (TicketStatus)r.Status, r.Redeemed, r.RedeemedEventId, r.CreatedAt)).ToList();
    }

    public async Task SetTicketStatusAsync(Guid uuid, TicketStatus status)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE SeasonSingleTickets SET Status = @0 WHERE Uuid = @1", (int)status, uuid.ToString());
    }

    public async Task<bool> ReferenceExistsAsync(int seasonId, string reference)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await ReferenceExistsAsync(scope.Database, seasonId, (reference ?? "").Trim());
    }

    public async Task<FlexTicketBundleView> CreateAsync(int seasonId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null)
    {
        if (quantity < 1) throw new DomainException("Die Anzahl muss mindestens 1 sein.");
        if (quantity > MaxBundleSize) throw new DomainException($"Die Anzahl darf höchstens {MaxBundleSize} sein.");

        var bundle = FlexTicketBundle.Create(seasonId, category, reference, createdByName, createdByEmail);

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        if (await ReferenceExistsAsync(db, seasonId, bundle.Reference))
            throw new DomainException($"Die Referenz „{bundle.Reference}“ ist in dieser Saison bereits vergeben.");

        var record = new FlexTicketBundleRecord
        {
            SeasonId = bundle.SeasonId,
            Category = (int)bundle.Category,
            Reference = bundle.Reference,
            CreatedAt = bundle.CreatedAt,
            CreatedByName = bundle.CreatedByName,
            CreatedByEmail = bundle.CreatedByEmail
        };
        await db.InsertAsync(record);

        for (var i = 0; i < quantity; i++)
        {
            var ticket = SeasonSingleTicket.CreateForBundle(seasonId, category, 0m, record.Id);
            await db.InsertAsync(new SeasonSingleTicketRecord
            {
                Uuid = ticket.Uuid.ToString(),
                SeasonId = ticket.SeasonId,
                Category = (int)ticket.Category,
                Price = ticket.Price,
                OrderId = ticket.OrderId,
                Status = (int)ticket.Status,
                CreatedAt = ticket.CreatedAt,
                RedeemedEventId = ticket.RedeemedEventId,
                Redeemed = ticket.Redeemed,
                BundleId = ticket.BundleId
            });
        }

        return new FlexTicketBundleView(record.Id, record.SeasonId, category, record.Reference,
            record.CreatedAt, quantity, 0, record.CreatedByName, record.CreatedByEmail);
    }

    private static async Task<bool> ReferenceExistsAsync(IDatabase db, int seasonId, string reference)
    {
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM FlexTicketBundles WHERE SeasonId = @0 AND Reference = @1", seasonId, reference);
        return count > 0;
    }

    private sealed class BundleCountRow
    {
        public int BundleId { get; set; }
        public int Total { get; set; }
        public int Redeemed { get; set; }
    }
}
