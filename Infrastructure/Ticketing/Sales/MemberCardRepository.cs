using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class MemberCardRepository(IScopeProvider scopeProvider) : IMemberCards
{
    public async Task<int> ImportAsync(int seasonId, string reference, IReadOnlyList<MemberImportRow> rows,
        string? createdByName = null, string? createdByEmail = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (string.IsNullOrWhiteSpace(reference)) throw new DomainException("Eine Referenz muss angegeben werden.");
        if (rows.Count == 0) return 0;

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var created = 0;
        foreach (var row in rows)
        {
            var card = MemberCard.Create(seasonId, row.Category, row.FirstName, row.LastName, row.Birthday,
                reference, createdByName: createdByName, createdByEmail: createdByEmail);
            await InsertAsync(scope.Database, card);
            created++;
        }
        return created;
    }

    public async Task CreateAsync(int seasonId, MemberCategory category, string? firstName, string? lastName,
        DateOnly? birthday, string reference, string? createdByName = null, string? createdByEmail = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        var reff = (reference ?? "").Trim();
        if (reff.Length == 0) throw new DomainException("Eine Referenz muss angegeben werden.");
        if (await ReferenceExistsAsync(seasonId, reff))
            throw new DomainException($"Die Referenz „{reff}“ ist in dieser Saison bereits vergeben.");

        var card = MemberCard.Create(seasonId, category, firstName, lastName, birthday, reff,
            createdByName: createdByName, createdByEmail: createdByEmail);

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await InsertAsync(scope.Database, card);
    }

    private static async Task InsertAsync(IDatabase db, MemberCard card)
    {
        var record = ToRecord(card);
        record.Uuid = (await TicketCode.AllocateAsync(db, card.Uuid)).ToString();
        await db.InsertAsync(record);
    }

    public async Task<bool> ReferenceExistsAsync(int seasonId, string reference)
    {
        var reff = (reference ?? "").Trim();
        if (reff.Length == 0) return false;

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var count = await scope.Database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM MembershipCards WHERE SeasonId = @0 AND Reference = @1", seasonId, reff);
        return count > 0;
    }

    public async Task<IReadOnlyList<string>> GetReferencesAsync()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await scope.Database.FetchAsync<string>(
            "SELECT DISTINCT Reference FROM MembershipCards " +
            "WHERE Reference IS NOT NULL AND Reference <> '' ORDER BY Reference");
    }

    public async Task<IReadOnlyList<MemberCard>> GetByReferenceAsync(string reference)
    {
        var reff = (reference ?? "").Trim();
        if (reff.Length == 0) return [];

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var records = await scope.Database.FetchAsync<MemberCardRecord>(
            "SELECT * FROM MembershipCards WHERE Reference = @0 ORDER BY LastName, FirstName", reff);
        return records.Select(Map).ToList();
    }

    private static MemberCard Map(MemberCardRecord r) => MemberCard.FromPersistence(
        r.Id,
        Guid.TryParse(r.Uuid, out var uuid) ? uuid : Guid.Empty,
        r.SeasonId,
        (MemberCategory)r.Category,
        r.OrderId,
        (TicketStatus)r.Status,
        r.CreatedAt,
        r.FirstName,
        r.LastName,
        r.Birthday is { } b ? DateOnly.FromDateTime(b) : null,
        r.Reference,
        r.CreatedByName,
        r.CreatedByEmail);

    private static MemberCardRecord ToRecord(MemberCard card) => new()
    {
        Uuid = card.Uuid.ToString(),
        SeasonId = card.SeasonId,
        Category = (int)card.Category,
        OrderId = card.OrderId,
        Status = (int)card.Status,
        CreatedAt = card.CreatedAt,
        FirstName = card.FirstName,
        LastName = card.LastName,
        Birthday = card.Birthday is { } b ? b.ToDateTime(TimeOnly.MinValue) : null,
        Reference = card.Reference,
        CreatedByName = card.CreatedByName,
        CreatedByEmail = card.CreatedByEmail
    };
}
