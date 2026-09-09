using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Features.Tickets.Infrastructure;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.FlexTickets.Infrastructure;

public sealed class FlexTicketBundleRepository(IScopeProvider scopeProvider) : IFlexTicketBundleRepository
{
    public async Task<FlexTicketBundle?> GetByIdAsync(int bundleId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var record = await scope.Database.FirstOrDefaultAsync<FlexTicketBundleRecord>("WHERE Id = @0", bundleId);
        return record is null
            ? null
            : FlexTicketBundle.FromPersistence(record.Id, record.SeasonId, (TicketCategory)record.Category, record.Reference,
                record.CreatedAt, record.CreatedByName, record.CreatedByEmail);
    }

    public async Task SaveAsync(FlexTicketBundle bundle)
    {
        if (bundle.Id <= 0) throw new DomainException("Das Bundle ist noch nicht gespeichert.");
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync("UPDATE FlexTicketBundles SET Reference = @0, Category = @1 WHERE Id = @2",
            bundle.Reference, (int)bundle.Category, bundle.Id);
    }

    public const int MaxBundleSize = 2000;

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
            CreatedAt = SwissTime.Timestamp,
            CreatedByName = null,
            CreatedByEmail = null
        };
        await db.InsertAsync(record);
        return record.Id;
    }

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

    public async Task<int> CreateAsync(int seasonId, TicketCategory category, string reference, int quantity,
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

        return record.Id;
    }

    public async Task<int> AddTicketsAsync(int bundleId, TicketCategory category, int quantity,
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

        return record.Id;
    }

    public async Task<int> CreateEmptyAsync(int seasonId, TicketCategory category, string reference,
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

        return record.Id;
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

    public async Task<Guid> CreateSingleAsync(int seasonId, TicketCategory category, string reference, CardHolder holder,
        string? createdByName = null, string? createdByEmail = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        var reff = (reference ?? "").Trim();
        if (reff.Length == 0) throw new DomainException("Ein Bundle muss angegeben werden.");

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var record = await db.FirstOrDefaultAsync<FlexTicketBundleRecord>(
            "WHERE SeasonId = @0 AND Reference = @1", seasonId, reff);
        if (record is null)
        {
            var bundle = FlexTicketBundle.Create(seasonId, category, reff, createdByName, createdByEmail);
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

        var ticket = SeasonSingleTicket.CreateForBundle(seasonId, category, 0m, record.Id);
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
            BuyerType = (int)holder.Type,
            BuyerFirstName = holder.FirstName,
            BuyerLastName = holder.LastName,
            BuyerCompany = holder.Company,
            BuyerEmail = holder.Email,
            Salutation = holder.Salutation,
            Birthday = holder.Birthday is { } b ? b.ToDateTime(TimeOnly.MinValue) : null,
            Street = holder.Street,
            AddressLine2 = holder.AddressLine2,
            PostalCode = holder.PostalCode,
            City = holder.City,
            Country = holder.Country,
            Phone = holder.Phone
        });
        return uuid;
    }

    public async Task SetHolderAsync(Guid uuid, CardHolder holder)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE SeasonSingleTickets SET BuyerType=@0, BuyerFirstName=@1, BuyerLastName=@2, BuyerCompany=@3, BuyerEmail=@4, " +
            "Salutation=@5, Birthday=@6, Street=@7, AddressLine2=@8, PostalCode=@9, City=@10, Country=@11, Phone=@12 " +
            "WHERE Uuid=@13",
            (object[])new object?[]
            {
                (int)holder.Type, holder.FirstName, holder.LastName, holder.Company, holder.Email,
                holder.Salutation, holder.Birthday is { } b ? b.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                holder.Street, holder.AddressLine2, holder.PostalCode, holder.City, holder.Country, holder.Phone,
                uuid.ToString()
            });
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
}
