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
            SELECT sp.Uuid, sp.Category, sp.Price, sp.Status, sp.CreatedAt,
                   sp.BuyerType, sp.BuyerFirstName, sp.BuyerLastName, sp.BuyerCompany, sp.CreatedByName,
                   o.OrderNumber AS OrderNumber, o.Status AS OrderStatus,
                   o.BillingFirstName AS BillingFirstName, o.BillingLastName AS BillingLastName
            FROM SeasonPasses sp
            LEFT JOIN Orders o ON o.Id = sp.OrderId
            WHERE sp.SeasonId = @0
            ORDER BY sp.CreatedAt DESC", new object[] { seasonId });

        var visitRows = await scope.Database.FetchAsync<UuidCountRow>(
            "SELECT TicketUuid AS Uuid, COUNT(DISTINCT EventId) AS Cnt FROM TicketEventVisits " +
            "WHERE TicketType = @0 AND TicketUuid IS NOT NULL GROUP BY TicketUuid",
            new object[] { (int)TicketType.SeasonPass });
        var visits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in visitRows)
            if (v.Uuid is not null) visits[v.Uuid] = v.Cnt;

        return passes.Select(p =>
        {
            var buyer = Buyer.FromPersistence(p.BuyerType ?? 0, p.BuyerFirstName, p.BuyerLastName, p.BuyerCompany);
            return new SeasonPassListItem(
                Guid.TryParse(p.Uuid, out var g) ? g : Guid.Empty,
                (TicketCategory)p.Category,
                p.Price,
                (TicketStatus)p.Status,
                p.CreatedAt,
                visits.GetValueOrDefault(p.Uuid),
                buyer?.DisplayName ?? BuyerName(p.BillingFirstName, p.BillingLastName),
                p.OrderNumber,
                p.OrderStatus is { } os ? PaymentState((OrderStatus)os) : null,
                buyer?.Type,
                p.CreatedByName);
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
        public decimal Price { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? BuyerType { get; set; }
        public string? BuyerFirstName { get; set; }
        public string? BuyerLastName { get; set; }
        public string? BuyerCompany { get; set; }
        public string? CreatedByName { get; set; }
        public string? OrderNumber { get; set; }
        public int? OrderStatus { get; set; }
        public string? BillingFirstName { get; set; }
        public string? BillingLastName { get; set; }
    }

    public sealed class UuidCountRow
    {
        public string? Uuid { get; set; }
        public int Cnt { get; set; }
    }
}

public sealed class SeasonPassAdminReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISeasonPassAdminReport, SeasonPassAdminReportReader>();
}
