using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class MemberCardRepository(IScopeProvider scopeProvider) : IMemberCards
{
    public async Task<int> ImportAsync(int seasonId, string reference, IReadOnlyList<MemberImportRow> rows,
        string? createdByName = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (string.IsNullOrWhiteSpace(reference)) throw new DomainException("Eine Referenz muss angegeben werden.");
        if (rows.Count == 0) return 0;

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var created = 0;
        foreach (var row in rows)
        {
            var card = MemberCard.Create(seasonId, row.Category, row.FirstName, row.LastName, row.Birthday,
                reference, createdByName: createdByName);
            await scope.Database.InsertAsync(new MemberCardRecord
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
                CreatedByName = card.CreatedByName
            });
            created++;
        }
        return created;
    }
}
