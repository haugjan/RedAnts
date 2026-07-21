using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class EventTicketBundleRepository(IScopeProvider scopeProvider) : IEventTicketBundles
{
    public const int MaxBundleSize = 1000;

    public async Task<IReadOnlyList<EventTicketBundleView>> GetByEventAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var bundles = await scope.Database.FetchAsync<EventTicketBundleRecord>(
            "WHERE EventId = @0 ORDER BY CreatedAt DESC", eventId);
        if (bundles.Count == 0) return [];

        var counts = await scope.Database.FetchAsync<BundleCountRow>(
            "SELECT BundleId AS BundleId, COUNT(*) AS Total, " +
            "SUM(CASE WHEN Redeemed = 1 THEN 1 ELSE 0 END) AS Redeemed " +
            "FROM EventTickets WHERE EventId = @0 AND BundleId IS NOT NULL GROUP BY BundleId",
            eventId);
        var byBundle = counts.ToDictionary(c => c.BundleId, c => c);

        return bundles.Select(b =>
        {
            var c = byBundle.GetValueOrDefault(b.Id);
            return new EventTicketBundleView(b.Id, b.EventId, (TicketCategory)b.Category, b.Reference,
                b.CreatedAt, c?.Total ?? 0, c?.Redeemed ?? 0, b.CreatedByName, b.CreatedByEmail);
        }).ToList();
    }

    public async Task<bool> ReferenceExistsAsync(int eventId, string reference)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await ReferenceExistsAsync(scope.Database, eventId, (reference ?? "").Trim());
    }

    public async Task<EventTicketBundleView> CreateAsync(int eventId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null)
    {
        if (quantity < 1) throw new DomainException("Die Anzahl muss mindestens 1 sein.");
        if (quantity > MaxBundleSize) throw new DomainException($"Die Anzahl darf höchstens {MaxBundleSize} sein.");

        var bundle = EventTicketBundle.Create(eventId, category, reference, createdByName, createdByEmail);

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        if (await ReferenceExistsAsync(db, eventId, bundle.Reference))
            throw new DomainException($"Der Bundlename „{bundle.Reference}“ ist für diesen Anlass bereits vergeben.");

        var record = new EventTicketBundleRecord
        {
            EventId = bundle.EventId,
            Category = (int)bundle.Category,
            Reference = bundle.Reference,
            CreatedAt = bundle.CreatedAt,
            CreatedByName = bundle.CreatedByName,
            CreatedByEmail = bundle.CreatedByEmail
        };
        await db.InsertAsync(record);

        for (var i = 0; i < quantity; i++)
        {
            var ticket = EventTicket.CreateForBundle(eventId, category, record.Id, bundle.CreatedByName, bundle.CreatedByEmail, orderId: orderId);
            await db.InsertAsync(new EventTicketRecord
            {
                Uuid = ticket.Uuid.ToString(),
                EventId = ticket.EventId,
                Category = (int)ticket.Category,
                Price = ticket.Price,
                OrderId = ticket.OrderId,
                Status = (int)ticket.Status,
                CreatedAt = ticket.CreatedAt,
                Redeemed = ticket.Redeemed,
                CreatedByName = ticket.CreatedByName,
                CreatedByEmail = ticket.CreatedByEmail,
                BundleId = ticket.BundleId
            });
        }

        return new EventTicketBundleView(record.Id, record.EventId, category, record.Reference,
            record.CreatedAt, quantity, 0, record.CreatedByName, record.CreatedByEmail);
    }

    private static async Task<bool> ReferenceExistsAsync(IDatabase db, int eventId, string reference)
    {
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM EventTicketBundles WHERE EventId = @0 AND Reference = @1", eventId, reference);
        return count > 0;
    }

    private sealed class BundleCountRow
    {
        public int BundleId { get; set; }
        public int Total { get; set; }
        public int Redeemed { get; set; }
    }
}
