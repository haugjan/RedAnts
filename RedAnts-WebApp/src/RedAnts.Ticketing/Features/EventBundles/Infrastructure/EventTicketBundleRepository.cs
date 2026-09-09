using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;
using RedAnts.Ticketing.Features.Tickets.Infrastructure;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.EventBundles.Infrastructure;

public sealed class EventTicketBundleRepository(IScopeProvider scopeProvider) : IEventTicketBundleRepository
{
    public const int MaxBundleSize = 1000;

    public async Task<bool> ReferenceExistsAsync(int eventId, string reference)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await ReferenceExistsAsync(scope.Database, eventId, (reference ?? "").Trim());
    }

    public async Task<int> CreateAsync(int eventId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null)
    {
        if (quantity < 1) throw new DomainException("Die Anzahl muss mindestens 1 sein.");
        if (quantity > MaxBundleSize) throw new DomainException($"Die Anzahl darf höchstens {MaxBundleSize} sein.");

        var bundle = EventTicketBundle.Create(eventId, category, reference, createdByName, createdByEmail);

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var record = await db.FirstOrDefaultAsync<EventTicketBundleRecord>(
            "WHERE EventId = @0 AND Reference = @1", eventId, bundle.Reference);
        if (record is null)
        {
            record = new EventTicketBundleRecord
            {
                EventId = bundle.EventId,
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
            var ticket = EventTicket.CreateForBundle(eventId, category, record.Id, bundle.CreatedByName, bundle.CreatedByEmail, orderId: orderId);
            var uuid = await TicketCode.AllocateAsync(db, ticket.Uuid);
            await db.InsertAsync(new EventTicketRecord
            {
                Uuid = uuid.ToString(),
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

        return record.Id;
    }

    public async Task<(int Created, int Updated)> ImportUnifiedAsync(int eventId, IReadOnlyList<TicketImportRow> rows,
        string defaultBundle, TicketCategory defaultCategory,
        string? createdByName = null, string? createdByEmail = null)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
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
            var record = await db.FirstOrDefaultAsync<EventTicketBundleRecord>(
                "WHERE EventId = @0 AND Reference = @1", eventId, reference);
            if (record is null)
            {
                var bundle = EventTicketBundle.Create(eventId, category, reference, createdByName, createdByEmail);
                record = new EventTicketBundleRecord
                {
                    EventId = bundle.EventId,
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
                    "UPDATE EventTickets SET Category=@0, BundleId=@1, BuyerType=@2, BuyerFirstName=@3, " +
                    "BuyerLastName=@4, BuyerCompany=@5, Email=@6, Salutation=@7, Birthday=@8, Street=@9, " +
                    "AddressLine2=@10, PostalCode=@11, City=@12, Country=@13, Phone=@14 " +
                    "WHERE EventId=@15 AND Uuid LIKE @16",
                    (object[])new object?[]
                    {
                        (int)category, bundleId, buyerType, h.FirstName, h.LastName, h.Company, h.Email,
                        h.Salutation, birthday, h.Street, h.AddressLine2, h.PostalCode, h.City, h.Country, h.Phone,
                        eventId, code + "%"
                    });
                if (affected > 0) { updated++; continue; }
            }

            var ticket = EventTicket.CreateForBundle(eventId, category, bundleId, createdByName, createdByEmail);
            var uuid = await TicketCode.AllocateAsync(db, ticket.Uuid);
            await db.InsertAsync(new EventTicketRecord
            {
                Uuid = uuid.ToString(),
                EventId = ticket.EventId,
                Category = (int)ticket.Category,
                Price = ticket.Price,
                OrderId = ticket.OrderId,
                Status = (int)ticket.Status,
                CreatedAt = ticket.CreatedAt,
                Redeemed = ticket.Redeemed,
                CreatedByName = ticket.CreatedByName,
                CreatedByEmail = ticket.CreatedByEmail,
                BundleId = ticket.BundleId,
                BuyerType = buyerType,
                BuyerFirstName = h.FirstName,
                BuyerLastName = h.LastName,
                BuyerCompany = h.Company,
                Email = h.Email,
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

    private static async Task<bool> ReferenceExistsAsync(IDatabase db, int eventId, string reference)
    {
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM EventTicketBundles WHERE EventId = @0 AND Reference = @1", eventId, reference);
        return count > 0;
    }
}
