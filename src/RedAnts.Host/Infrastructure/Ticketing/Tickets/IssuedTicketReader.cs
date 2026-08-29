using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Tickets;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class IssuedTicketReader(IScopeProvider scopeProvider) : IIssuedTicketReader
{
    public async Task<IssuedTicket?> FindAsync(Guid uuid)
    {
        var key = uuid.ToString();
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var eventTicket = await db.FirstOrDefaultAsync<EventTicketRecord>("WHERE Uuid = @0", key);
        if (eventTicket is not null)
            return new IssuedTicket(TicketType.EventTicket, uuid, eventTicket.EventId,
                (TicketCategory)eventTicket.Category, (TicketStatus)eventTicket.Status, eventTicket.CreatedAt, null,
                BuyerName: BuyerLabel(eventTicket.BuyerType, eventTicket.BuyerFirstName, eventTicket.BuyerLastName, eventTicket.BuyerCompany),
                CategoryName: await CategoryLabel(db, eventTicket.TierId, eventTicket.Category));

        var single = await db.FirstOrDefaultAsync<SeasonSingleTicketRecord>("WHERE Uuid = @0", key);
        if (single is not null)
            return new IssuedTicket(TicketType.SeasonSingle, uuid, single.SeasonId,
                (TicketCategory)single.Category, (TicketStatus)single.Status, single.CreatedAt, null,
                CategoryName: await CategoryLabel(db, single.TierId, single.Category));

        var pass = await db.FirstOrDefaultAsync<SeasonPassRecord>("WHERE Uuid = @0", key);
        if (pass is not null)
            return new IssuedTicket(TicketType.SeasonPass, uuid, pass.SeasonId,
                (TicketCategory)pass.Category, (TicketStatus)pass.Status, pass.CreatedAt, null,
                BuyerName: BuyerLabel(pass.BuyerType, pass.BuyerFirstName, pass.BuyerLastName, pass.BuyerCompany),
                CategoryName: await CategoryLabel(db, pass.TierId, pass.Category));

        var card = await db.FirstOrDefaultAsync<MemberCardRecord>("WHERE Uuid = @0", key);
        if (card is not null)
        {
            var holder = string.IsNullOrWhiteSpace(card.Company)
                ? string.Join(' ', new[] { card.FirstName, card.LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s)))
                : card.Company.Trim();
            return new IssuedTicket(TicketType.MemberCard, uuid, card.SeasonId,
                null, (TicketStatus)card.Status, card.CreatedAt,
                string.IsNullOrWhiteSpace(holder) ? null : holder,
                (MemberCategory)card.Category,
                Birthday: card.Birthday is { } b ? DateOnly.FromDateTime(b) : null,
                Admissions: card.Admissions);
        }

        return null;
    }

    public async Task<IssuedTicket?> FindByCodeAsync(string codePrefix)
    {
        var code = (codePrefix ?? "").Trim().ToLowerInvariant();
        if (code.Length != 8 || !code.All(Uri.IsHexDigit)) return null;

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var pattern = code + "%";
        var uuid = await scope.Database.ExecuteScalarAsync<string?>(
            "SELECT TOP 1 Uuid FROM (" +
            "SELECT Uuid FROM EventTickets WHERE Uuid LIKE @0 " +
            "UNION ALL SELECT Uuid FROM SeasonSingleTickets WHERE Uuid LIKE @0 " +
            "UNION ALL SELECT Uuid FROM SeasonPasses WHERE Uuid LIKE @0 " +
            "UNION ALL SELECT Uuid FROM MembershipCards WHERE Uuid LIKE @0) t", pattern);

        return Guid.TryParse(uuid, out var parsed) ? await FindAsync(parsed) : null;
    }

    private static async Task<string?> CategoryLabel(IDatabase db, int? tierId, int category)
    {
        if (tierId is { } id)
        {
            var tier = await db.FirstOrDefaultAsync<SeasonPriceTierRecord>("WHERE Id = @0", id);
            if (tier is not null)
            {
                if (tier.PromoOfTierId is { } parentId)
                {
                    var parentName = await db.ExecuteScalarAsync<string?>(
                        "SELECT Name FROM SeasonPriceTiers WHERE Id = @0", parentId);
                    if (!string.IsNullOrWhiteSpace(parentName)) return parentName;
                }
                if (!string.IsNullOrWhiteSpace(tier.Name)) return tier.Name;
            }
        }
        return ((TicketCategory)category).DisplayName();
    }

    private static string? BuyerLabel(int? type, string? first, string? last, string? company)
    {
        var buyer = Buyer.FromPersistence(type ?? 0, first, last, company);
        return string.IsNullOrWhiteSpace(buyer?.DisplayName) ? null : buyer!.DisplayName;
    }
}
