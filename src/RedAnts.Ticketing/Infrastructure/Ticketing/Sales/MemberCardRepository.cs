using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class MemberCardRepository(IScopeProvider scopeProvider) : IMemberCards
{
    public async Task<int> ImportAsync(int seasonId, string reference, MemberCategory category, IReadOnlyList<MemberImportRow> rows,
        string? createdByName = null, string? createdByEmail = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (string.IsNullOrWhiteSpace(reference)) throw new DomainException("Ein Bundle muss angegeben werden.");
        if (rows.Count == 0) return 0;

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var affected = 0;
        foreach (var row in rows)
        {
            var overrideReference = string.IsNullOrWhiteSpace(row.Reference) ? null : row.Reference.Trim();
            var rowReference = overrideReference ?? reference;

            if (!string.IsNullOrWhiteSpace(row.CardNo)
                && await TryUpdateByCodeAsync(scope.Database, seasonId, category, row, overrideReference))
            {
                affected++;
                continue;
            }

            var card = MemberCard.Create(seasonId, category, row.FirstName, row.LastName, row.Birthday,
                email: row.Email, reference: rowReference, createdByName: createdByName, createdByEmail: createdByEmail,
                address: row.Address, admissions: row.Admissions);
            await InsertAsync(scope.Database, card);
            affected++;
        }
        return affected;
    }

    private static async Task<bool> TryUpdateByCodeAsync(IDatabase db, int seasonId, MemberCategory category,
        MemberImportRow row, string? overrideReference)
    {
        var code = (row.CardNo ?? "").Trim().ToLowerInvariant();
        if (code.Length != 8 || !code.All(Uri.IsHexDigit)) return false;

        var addr = row.Address ?? MemberAddress.Empty;
        var affected = await db.ExecuteAsync(
            "UPDATE MembershipCards SET FirstName=@0, LastName=@1, Birthday=@2, Category=@3, Email=@4, " +
            "Salutation=@5, Company=@6, Street=@7, AddressLine2=@8, PostalCode=@9, City=@10, Country=@11, Phone=@12, " +
            "Admissions=@13, Reference=ISNULL(@16, Reference) WHERE SeasonId=@14 AND Uuid LIKE @15",
            (object[])new object?[]
            {
                Clean(row.FirstName), Clean(row.LastName),
                row.Birthday is { } b ? b.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                (int)category, Clean(row.Email),
                addr.Salutation, addr.Company, addr.Street, addr.AddressLine2,
                addr.PostalCode, addr.City, addr.Country, addr.Phone,
                Math.Max(1, row.Admissions), seasonId, code + "%", overrideReference
            });
        return affected > 0;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task CreateAsync(int seasonId, MemberCategory category, string? firstName, string? lastName,
        DateOnly? birthday, string reference, string? email = null, string? createdByName = null,
        string? createdByEmail = null, MemberAddress? address = null, int admissions = 1)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        var reff = (reference ?? "").Trim();
        if (reff.Length == 0) throw new DomainException("Ein Bundle muss angegeben werden.");

        var card = MemberCard.Create(seasonId, category, firstName, lastName, birthday,
            email: email, reference: reff, createdByName: createdByName, createdByEmail: createdByEmail,
            address: address, admissions: admissions);

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
        r.Email,
        r.Reference,
        r.CreatedByName,
        r.CreatedByEmail,
        MemberAddress.Create(r.Salutation, r.Company, r.Street, r.AddressLine2,
            r.PostalCode, r.City, r.Country, r.Phone),
        r.Admissions);

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
        Email = card.Email,
        Reference = card.Reference,
        Admissions = card.Admissions,
        CreatedByName = card.CreatedByName,
        CreatedByEmail = card.CreatedByEmail,
        Salutation = card.Address.Salutation,
        Company = card.Address.Company,
        Street = card.Address.Street,
        AddressLine2 = card.Address.AddressLine2,
        PostalCode = card.Address.PostalCode,
        City = card.Address.City,
        Country = card.Address.Country,
        Phone = card.Address.Phone
    };
}
