using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class SeasonPassAdminReportReader(IScopeProvider scopeProvider) : ISeasonPassAdminReport
{
    public async Task<IReadOnlyList<SeasonPassListItem>> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var passes = await scope.Database.FetchAsync<Row>(@"
            SELECT sp.Uuid, sp.Category, sp.TierId, sp.Price, sp.Status, sp.CreatedAt,
                   sp.BuyerType, sp.BuyerFirstName, sp.BuyerLastName, sp.BuyerCompany, sp.CreatedByName, sp.Reference,
                   sp.BuyerEmail AS BuyerEmail,
                   sp.Salutation, sp.Birthday, sp.Street, sp.AddressLine2, sp.PostalCode, sp.City, sp.Country, sp.Phone,
                   o.OrderNumber AS OrderNumber, o.Status AS OrderStatus,
                   o.BillingFirstName AS BillingFirstName, o.BillingLastName AS BillingLastName,
                   o.BillingEmail AS BillingEmail
            FROM SeasonPasses sp
            LEFT JOIN Orders o ON o.Id = sp.OrderId
            WHERE sp.SeasonId = @0
            ORDER BY sp.CreatedAt DESC", new object[] { seasonId });

        var tiers = (await scope.Database.FetchAsync<TierNameRow>(
                "SELECT Id, Name, PromoOfTierId FROM SeasonPriceTiers WHERE SeasonId = @0", new object[] { seasonId }))
            .ToDictionary(t => t.Id);

        string ResolveCategory(int? tierId)
        {
            if (tierId is not { } tid || !tiers.TryGetValue(tid, out var t)) return "–";
            if (t.PromoOfTierId is { } parent && tiers.TryGetValue(parent, out var pt)) return pt.Name;
            return t.Name;
        }

        var visitRows = await scope.Database.FetchAsync<UuidCountRow>(
            "SELECT TicketUuid AS Uuid, COUNT(DISTINCT EventId) AS Cnt FROM TicketEventVisits " +
            "WHERE TicketType = @0 AND TicketUuid IS NOT NULL GROUP BY TicketUuid",
            new object[] { (int)TicketType.SeasonPass });
        var visits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in visitRows)
            if (v.Uuid is not null) visits[v.Uuid] = v.Cnt;

        var convRows = await scope.Database.FetchAsync<UuidCountRow>(
            "SELECT OriginCardUuid AS Uuid, COUNT(*) AS Cnt FROM EventTickets " +
            "WHERE OriginType = @0 AND OriginCardUuid IS NOT NULL AND Status = @1 GROUP BY OriginCardUuid",
            new object[] { (int)TicketType.SeasonPass, (int)TicketStatus.Valid });
        var conversions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in convRows)
            if (v.Uuid is not null) conversions[v.Uuid] = v.Cnt;

        return passes.Select(p =>
        {
            var buyer = Buyer.FromPersistence(p.BuyerType ?? 0, p.BuyerFirstName, p.BuyerLastName, p.BuyerCompany);
            var email = string.IsNullOrWhiteSpace(p.BuyerEmail) ? p.BillingEmail : p.BuyerEmail;
            var holder = CardHolder.Create(
                (BuyerType)(p.BuyerType ?? 0), p.Salutation, buyer?.Company,
                buyer?.FirstName ?? p.BillingFirstName, buyer?.LastName ?? p.BillingLastName,
                p.Birthday is { } bd ? DateOnly.FromDateTime(bd) : null, email,
                p.Street, p.AddressLine2, p.PostalCode, p.City, p.Country, p.Phone);
            return new SeasonPassListItem(
                Guid.TryParse(p.Uuid, out var g) ? g : Guid.Empty,
                ResolveCategory(p.TierId),
                p.Price,
                (TicketStatus)p.Status,
                p.CreatedAt,
                visits.GetValueOrDefault(p.Uuid),
                buyer?.DisplayName ?? BuyerName(p.BillingFirstName, p.BillingLastName),
                p.OrderNumber,
                p.OrderStatus is { } os ? PaymentState((OrderStatus)os) : null,
                buyer?.Type,
                p.CreatedByName,
                p.Reference,
                email,
                buyer?.FirstName ?? p.BillingFirstName,
                buyer?.LastName ?? p.BillingLastName,
                buyer?.Company,
                conversions.GetValueOrDefault(p.Uuid),
                p.TierId,
                holder);
        }).ToList();
    }

    private static string? BuyerName(string? first, string? last)
    {
        var name = $"{first} {last}".Trim();
        return string.IsNullOrEmpty(name) ? null : name;
    }

    private static string PaymentState(OrderStatus status) => status switch
    {
        OrderStatus.Paid => "bezahlt",
        OrderStatus.Draft => "offen",
        OrderStatus.Cancelled => "storniert",
        OrderStatus.Refunded => "erstattet",
        _ => status.ToString()
    };

    public sealed class Row
    {
        public string Uuid { get; set; } = "";
        public int Category { get; set; }
        public int? TierId { get; set; }
        public decimal Price { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? BuyerType { get; set; }
        public string? BuyerFirstName { get; set; }
        public string? BuyerLastName { get; set; }
        public string? BuyerCompany { get; set; }
        public string? CreatedByName { get; set; }
        public string? Reference { get; set; }
        public string? BuyerEmail { get; set; }
        public string? Salutation { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Street { get; set; }
        public string? AddressLine2 { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
        public string? OrderNumber { get; set; }
        public int? OrderStatus { get; set; }
        public string? BillingFirstName { get; set; }
        public string? BillingLastName { get; set; }
        public string? BillingEmail { get; set; }
    }

    public sealed class UuidCountRow
    {
        public string? Uuid { get; set; }
        public int Cnt { get; set; }
    }

    public sealed class TierNameRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? PromoOfTierId { get; set; }
    }
}

public sealed class SeasonPassAdminReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISeasonPassAdminReport, SeasonPassAdminReportReader>();
}
