using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class FlexTicketBundleRepository(IScopeProvider scopeProvider) : IFlexTicketBundles
{
    public const int MaxBundleSize = 2000;

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

    public async Task<FlexRebookResult> RebookByUuidAsync(int targetBundleId, Guid uuid, string? operatorName)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var ticket = await db.FirstOrDefaultAsync<SeasonSingleTicketRecord>("WHERE Uuid = @0", uuid.ToString());
        return await ApplyRebookAsync(db, targetBundleId, ticket);
    }

    public async Task<FlexRebookResult> RebookByCodeAsync(int targetBundleId, string codePrefix, string? operatorName)
    {
        var code = (codePrefix ?? "").Trim().ToLowerInvariant();
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var ticket = code.Length == 0
            ? null
            : await db.FirstOrDefaultAsync<SeasonSingleTicketRecord>("WHERE Uuid LIKE @0", code + "%");
        return await ApplyRebookAsync(db, targetBundleId, ticket);
    }

    private static async Task<FlexRebookResult> ApplyRebookAsync(IDatabase db, int targetBundleId, SeasonSingleTicketRecord? ticket)
    {
        var target = await db.FirstOrDefaultAsync<FlexTicketBundleRecord>("WHERE Id = @0", targetBundleId);
        if (target is null) return new FlexRebookResult(FlexRebookStatus.NotFound);
        if (ticket is null) return new FlexRebookResult(FlexRebookStatus.NotFound, ToBundle: target.Reference);

        var reference = ticket.Uuid.Length >= 8 ? ticket.Uuid[..8].ToUpperInvariant() : ticket.Uuid.ToUpperInvariant();
        var category = ((TicketCategory)ticket.Category).DisplayName();

        if (ticket.Redeemed || ticket.RedeemedEventId is not null)
            return new FlexRebookResult(FlexRebookStatus.AlreadyRedeemed, reference, category,
                ToBundle: target.Reference, TicketSeasonId: ticket.SeasonId);

        if (ticket.SeasonId != target.SeasonId)
            return new FlexRebookResult(FlexRebookStatus.WrongSeason, reference, category,
                ToBundle: target.Reference, TicketSeasonId: ticket.SeasonId);

        if (ticket.BundleId == targetBundleId)
            return new FlexRebookResult(FlexRebookStatus.AlreadyInTarget, reference, category,
                target.Reference, target.Reference, ticket.SeasonId);

        string? fromRef = null;
        if (ticket.BundleId is { } prev)
            fromRef = (await db.FirstOrDefaultAsync<FlexTicketBundleRecord>("WHERE Id = @0", prev))?.Reference;

        await db.ExecuteAsync("UPDATE SeasonSingleTickets SET BundleId = @0, BoxOffice = 0, OriginBundleId = NULL WHERE Uuid = @1", targetBundleId, ticket.Uuid);
        return new FlexRebookResult(FlexRebookStatus.Moved, reference, category, fromRef, target.Reference, ticket.SeasonId);
    }

    public async Task<FlexBoxOfficeResult> ConvertToBoxOfficeByUuidAsync(Guid uuid, string? operatorName)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var ticket = await db.FirstOrDefaultAsync<SeasonSingleTicketRecord>("WHERE Uuid = @0", uuid.ToString());
        return await ApplyBoxOfficeAsync(db, ticket);
    }

    public async Task<FlexBoxOfficeResult> ConvertToBoxOfficeByCodeAsync(string codePrefix, string? operatorName)
    {
        var code = (codePrefix ?? "").Trim().ToLowerInvariant();
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var ticket = code.Length == 0
            ? null
            : await db.FirstOrDefaultAsync<SeasonSingleTicketRecord>("WHERE Uuid LIKE @0", code + "%");
        return await ApplyBoxOfficeAsync(db, ticket);
    }

    private const string BoxOfficeBundleReference = "Abendkasse";

    private static async Task<FlexBoxOfficeResult> ApplyBoxOfficeAsync(IDatabase db, SeasonSingleTicketRecord? ticket)
    {
        if (ticket is null) return new FlexBoxOfficeResult(FlexBoxOfficeStatus.NotFound);

        var reference = ticket.Uuid.Length >= 8 ? ticket.Uuid[..8].ToUpperInvariant() : ticket.Uuid.ToUpperInvariant();
        var category = ((TicketCategory)ticket.Category).DisplayName();

        if (ticket.Redeemed || ticket.RedeemedEventId is not null)
            return new FlexBoxOfficeResult(FlexBoxOfficeStatus.AlreadyRedeemed, reference, category);

        var bundleId = await GetOrCreateBoxOfficeBundleAsync(db, ticket.SeasonId);

        if (ticket.BoxOffice && ticket.BundleId == bundleId)
            return new FlexBoxOfficeResult(FlexBoxOfficeStatus.AlreadyBoxOffice, reference, category);

        await db.ExecuteAsync(
            "UPDATE SeasonSingleTickets SET BoxOffice = 1, OriginBundleId = COALESCE(OriginBundleId, BundleId), BundleId = @0 WHERE Uuid = @1",
            bundleId, ticket.Uuid);
        return new FlexBoxOfficeResult(FlexBoxOfficeStatus.Converted, reference, category);
    }

    private static async Task<int> GetOrCreateBoxOfficeBundleAsync(IDatabase db, int seasonId)
    {
        var existing = await db.FirstOrDefaultAsync<FlexTicketBundleRecord>(
            "WHERE SeasonId = @0 AND Reference = @1", seasonId, BoxOfficeBundleReference);
        if (existing is not null) return existing.Id;

        var record = new FlexTicketBundleRecord
        {
            SeasonId = seasonId,
            Category = (int)TicketCategory.Adult,
            Reference = BoxOfficeBundleReference,
            CreatedAt = DateTime.UtcNow,
            CreatedByName = null,
            CreatedByEmail = null
        };
        await db.InsertAsync(record);
        return record.Id;
    }

    private const string TicketSelect =
        "SELECT t.Id, t.Uuid, t.Category, t.Status, t.Redeemed, t.RedeemedEventId, t.CreatedAt, t.BoxOffice, v.IsInside AS InsideFlag, " +
        "cb.CreatedByName AS CreatorName, cb.CreatedByEmail AS CreatorEmail, bb.Reference AS BundleReference, " +
        "t.BuyerType, t.BuyerFirstName, t.BuyerLastName, t.BuyerCompany, t.BuyerEmail, " +
        "t.Salutation, t.Birthday, t.Street, t.AddressLine2, t.PostalCode, t.City, t.Country, t.Phone, " +
        "CASE WHEN EXISTS (SELECT 1 FROM EventTickets et WHERE et.OriginType = @1 AND et.OriginCardUuid = t.Uuid) THEN 1 ELSE 0 END AS Converted " +
        "FROM SeasonSingleTickets t " +
        "LEFT JOIN TicketEventVisits v ON v.TicketUuid = t.Uuid AND v.EventId = t.RedeemedEventId " +
        "LEFT JOIN FlexTicketBundles cb ON cb.Id = COALESCE(t.OriginBundleId, CASE WHEN t.BoxOffice = 1 THEN NULL ELSE t.BundleId END) " +
        "LEFT JOIN FlexTicketBundles bb ON bb.Id = t.BundleId ";

    public async Task<IReadOnlyList<FlexTicketView>> GetTicketsAsync(int bundleId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<FlexTicketRow>(
            TicketSelect + "WHERE t.BundleId = @0 ORDER BY t.CreatedAt", bundleId, (int)TicketType.SeasonSingle);
        return rows.Select(MapTicket).ToList();
    }

    public async Task<IReadOnlyList<FlexTicketView>> GetTicketsBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<FlexTicketRow>(
            TicketSelect + "WHERE t.SeasonId = @0 ORDER BY t.CreatedAt", seasonId, (int)TicketType.SeasonSingle);
        return rows.Select(MapTicket).ToList();
    }

    private static FlexTicketView MapTicket(FlexTicketRow r) => new(
        Guid.TryParse(r.Uuid, out var uuid) ? uuid : Guid.Empty,
        (TicketStatus)r.Status, r.Redeemed, r.RedeemedEventId, r.CreatedAt,
        (TicketCategory)r.Category, r.InsideFlag, r.Converted == 1, r.BoxOffice,
        r.CreatorName, r.CreatorEmail,
        CardHolder.Create((BuyerType)(r.BuyerType ?? 0), r.Salutation, r.BuyerCompany,
            r.BuyerFirstName, r.BuyerLastName, r.Birthday is { } bd ? DateOnly.FromDateTime(bd) : null,
            r.BuyerEmail, r.Street, r.AddressLine2, r.PostalCode, r.City, r.Country, r.Phone),
        r.BundleReference);

    public async Task SetTicketCategoryAsync(Guid uuid, TicketCategory category)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE SeasonSingleTickets SET Category = @0 WHERE Uuid = @1", (int)category, uuid.ToString());
    }

    public async Task SetTicketStatusAsync(Guid uuid, TicketStatus status)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE SeasonSingleTickets SET Status = @0 WHERE Uuid = @1", (int)status, uuid.ToString());
    }

    public async Task SetTicketRedeemedAsync(Guid uuid, bool redeemed)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        if (redeemed)
            await scope.Database.ExecuteAsync(
                "UPDATE SeasonSingleTickets SET Redeemed = 1 WHERE Uuid = @0", uuid.ToString());
        else
            await scope.Database.ExecuteAsync(
                "UPDATE SeasonSingleTickets SET Redeemed = 0, RedeemedEventId = NULL WHERE Uuid = @0", uuid.ToString());
    }

    public async Task<bool> ReferenceExistsAsync(int seasonId, string reference)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await ReferenceExistsAsync(scope.Database, seasonId, (reference ?? "").Trim());
    }

    public async Task<FlexTicketBundleView> CreateAsync(int seasonId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null)
    {
        if (quantity < 1) throw new DomainException("Die Anzahl muss mindestens 1 sein.");
        if (quantity > MaxBundleSize) throw new DomainException($"Die Anzahl darf höchstens {MaxBundleSize} sein.");

        var bundle = FlexTicketBundle.Create(seasonId, category, reference, createdByName, createdByEmail);

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var record = await db.FirstOrDefaultAsync<FlexTicketBundleRecord>(
            "WHERE SeasonId = @0 AND Reference = @1", seasonId, bundle.Reference);
        if (record is null)
        {
            record = new FlexTicketBundleRecord
            {
                SeasonId = bundle.SeasonId,
                Category = (int)bundle.Category,
                Reference = bundle.Reference,
                CreatedAt = bundle.CreatedAt,
                CreatedByName = bundle.CreatedByName,
                CreatedByEmail = bundle.CreatedByEmail
            };
            await db.InsertAsync(record);
        }

        for (var i = 0; i < quantity; i++)
        {
            var ticket = SeasonSingleTicket.CreateForBundle(seasonId, category, 0m, record.Id, orderId: orderId);
            var uuid = await TicketCode.AllocateAsync(db, ticket.Uuid);
            await db.InsertAsync(new SeasonSingleTicketRecord
            {
                Uuid = uuid.ToString(),
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

        var total = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM SeasonSingleTickets WHERE BundleId = @0", record.Id);
        return new FlexTicketBundleView(record.Id, record.SeasonId, (TicketCategory)record.Category, record.Reference,
            record.CreatedAt, total, 0, record.CreatedByName, record.CreatedByEmail);
    }

    public async Task<FlexTicketBundleView> AddTicketsAsync(int bundleId, TicketCategory category, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null)
    {
        if (quantity < 1) throw new DomainException("Die Anzahl muss mindestens 1 sein.");
        if (quantity > MaxBundleSize) throw new DomainException($"Die Anzahl darf höchstens {MaxBundleSize} sein.");

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var record = await db.FirstOrDefaultAsync<FlexTicketBundleRecord>("WHERE Id = @0", bundleId)
            ?? throw new DomainException("Das Bundle wurde nicht gefunden.");

        var existing = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM SeasonSingleTickets WHERE BundleId = @0", bundleId);
        if (existing + quantity > MaxBundleSize)
            throw new DomainException($"Ein Bundle darf höchstens {MaxBundleSize} Tickets enthalten.");

        for (var i = 0; i < quantity; i++)
        {
            var ticket = SeasonSingleTicket.CreateForBundle(record.SeasonId, category, 0m, record.Id, orderId: orderId);
            var uuid = await TicketCode.AllocateAsync(db, ticket.Uuid);
            await db.InsertAsync(new SeasonSingleTicketRecord
            {
                Uuid = uuid.ToString(),
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

        var redeemed = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM SeasonSingleTickets WHERE BundleId = @0 AND Redeemed = 1", bundleId);
        return new FlexTicketBundleView(record.Id, record.SeasonId, (TicketCategory)record.Category, record.Reference,
            record.CreatedAt, existing + quantity, redeemed, record.CreatedByName, record.CreatedByEmail);
    }

    public async Task<FlexTicketBundleView> CreateEmptyAsync(int seasonId, TicketCategory category, string reference,
        string? createdByName = null, string? createdByEmail = null)
    {
        var bundle = FlexTicketBundle.Create(seasonId, category, reference, createdByName, createdByEmail);

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var record = await db.FirstOrDefaultAsync<FlexTicketBundleRecord>(
            "WHERE SeasonId = @0 AND Reference = @1", seasonId, bundle.Reference);
        if (record is null)
        {
            record = new FlexTicketBundleRecord
            {
                SeasonId = bundle.SeasonId,
                Category = (int)bundle.Category,
                Reference = bundle.Reference,
                CreatedAt = bundle.CreatedAt,
                CreatedByName = bundle.CreatedByName,
                CreatedByEmail = bundle.CreatedByEmail
            };
            await db.InsertAsync(record);
        }

        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM SeasonSingleTickets WHERE BundleId = @0", record.Id);
        return new FlexTicketBundleView(record.Id, record.SeasonId, (TicketCategory)record.Category, record.Reference,
            record.CreatedAt, count, 0, record.CreatedByName, record.CreatedByEmail);
    }

    public async Task<(int Created, int Updated)> ImportUnifiedAsync(int seasonId, IReadOnlyList<TicketImportRow> rows,
        string defaultBundle, TicketCategory defaultCategory,
        string? createdByName = null, string? createdByEmail = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        var fallbackBundle = (defaultBundle ?? "").Trim();
        if (fallbackBundle.Length == 0) throw new DomainException("Ein Bundle muss angegeben werden.");
        if (rows.Count == 0) return (0, 0);

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var bundleIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var created = 0;
        var updated = 0;

        async Task<int> BundleIdForAsync(string reference, TicketCategory category)
        {
            if (bundleIds.TryGetValue(reference, out var cached)) return cached;
            var record = await db.FirstOrDefaultAsync<FlexTicketBundleRecord>(
                "WHERE SeasonId = @0 AND Reference = @1", seasonId, reference);
            if (record is null)
            {
                var bundle = FlexTicketBundle.Create(seasonId, category, reference, createdByName, createdByEmail);
                record = new FlexTicketBundleRecord
                {
                    SeasonId = bundle.SeasonId,
                    Category = (int)bundle.Category,
                    Reference = bundle.Reference,
                    CreatedAt = bundle.CreatedAt,
                    CreatedByName = bundle.CreatedByName,
                    CreatedByEmail = bundle.CreatedByEmail
                };
                await db.InsertAsync(record);
            }
            bundleIds[reference] = record.Id;
            return record.Id;
        }

        foreach (var row in rows)
        {
            var reference = string.IsNullOrWhiteSpace(row.Bundle) ? fallbackBundle : row.Bundle.Trim();
            var category = TicketCategoryExtensions.ParseMainCategory(row.Category, defaultCategory);
            var bundleId = await BundleIdForAsync(reference, category);
            var h = row.Holder;
            var birthday = h.Birthday is { } b ? b.ToDateTime(TimeOnly.MinValue) : (DateTime?)null;
            var buyerType = (int)(h.IsCompany ? BuyerType.Company : BuyerType.Private);

            var code = (row.CardNo ?? "").Trim().ToLowerInvariant();
            if (code.Length == 8 && code.All(Uri.IsHexDigit))
            {
                var affected = await db.ExecuteAsync(
                    "UPDATE SeasonSingleTickets SET Category=@0, BundleId=@1, BuyerType=@2, BuyerFirstName=@3, " +
                    "BuyerLastName=@4, BuyerCompany=@5, BuyerEmail=@6, Salutation=@7, Birthday=@8, Street=@9, " +
                    "AddressLine2=@10, PostalCode=@11, City=@12, Country=@13, Phone=@14 " +
                    "WHERE SeasonId=@15 AND Uuid LIKE @16",
                    (object[])new object?[]
                    {
                        (int)category, bundleId, buyerType, h.FirstName, h.LastName, h.Company, h.Email,
                        h.Salutation, birthday, h.Street, h.AddressLine2, h.PostalCode, h.City, h.Country, h.Phone,
                        seasonId, code + "%"
                    });
                if (affected > 0) { updated++; continue; }
            }

            var ticket = SeasonSingleTicket.CreateForBundle(seasonId, category, 0m, bundleId);
            var uuid = await TicketCode.AllocateAsync(db, ticket.Uuid);
            await db.InsertAsync(new SeasonSingleTicketRecord
            {
                Uuid = uuid.ToString(),
                SeasonId = ticket.SeasonId,
                Category = (int)ticket.Category,
                Price = ticket.Price,
                OrderId = ticket.OrderId,
                Status = (int)ticket.Status,
                CreatedAt = ticket.CreatedAt,
                RedeemedEventId = ticket.RedeemedEventId,
                Redeemed = ticket.Redeemed,
                BundleId = ticket.BundleId,
                BuyerType = buyerType,
                BuyerFirstName = h.FirstName,
                BuyerLastName = h.LastName,
                BuyerCompany = h.Company,
                BuyerEmail = h.Email,
                Salutation = h.Salutation,
                Birthday = birthday,
                Street = h.Street,
                AddressLine2 = h.AddressLine2,
                PostalCode = h.PostalCode,
                City = h.City,
                Country = h.Country,
                Phone = h.Phone
            });
            created++;
        }

        return (created, updated);
    }

    public async Task<bool> DeleteEmptyAsync(int bundleId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM SeasonSingleTickets WHERE BundleId = @0", bundleId);
        if (count > 0) return false;
        await db.ExecuteAsync("DELETE FROM FlexTicketBundles WHERE Id = @0", bundleId);
        return true;
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

    private sealed class FlexTicketRow
    {
        public int Id { get; set; }
        public string Uuid { get; set; } = "";
        public int Category { get; set; }
        public int Status { get; set; }
        public bool Redeemed { get; set; }
        public int? RedeemedEventId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool? InsideFlag { get; set; }
        public int Converted { get; set; }
        public bool BoxOffice { get; set; }
        public string? CreatorName { get; set; }
        public string? CreatorEmail { get; set; }
        public string? BundleReference { get; set; }
        public int? BuyerType { get; set; }
        public string? BuyerFirstName { get; set; }
        public string? BuyerLastName { get; set; }
        public string? BuyerCompany { get; set; }
        public string? BuyerEmail { get; set; }
        public string? Salutation { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Street { get; set; }
        public string? AddressLine2 { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
    }
}
