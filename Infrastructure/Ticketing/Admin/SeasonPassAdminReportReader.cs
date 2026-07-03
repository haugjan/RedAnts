using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

/// <summary>Lists a season's passes from the <c>SeasonPasses</c> table, left-joins the buyer/payment
/// from the (immutable) <c>Orders</c> record, and joins each pass's distinct-event visit count from
/// <c>TicketEventVisits</c>. Independent of the ticket repositories.</summary>
public sealed class SeasonPassAdminReportReader(IScopeProvider scopeProvider) : ISeasonPassAdminReport
{
    public async Task<IReadOnlyList<SeasonPassListItem>> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var passes = await scope.Database.FetchAsync<Row>(@"
            SELECT sp.Uuid, sp.Category, sp.Price, sp.Status, sp.CreatedAt,
                   o.OrderNumber AS OrderNumber, o.Status AS OrderStatus,
                   o.BillingFirstName AS BillingFirstName, o.BillingLastName AS BillingLastName
            FROM SeasonPasses sp
            LEFT JOIN Orders o ON o.Id = sp.OrderId
            WHERE sp.SeasonId = @0
            ORDER BY sp.CreatedAt DESC", new object[] { seasonId });

        // Distinct events visited per season pass (from the shared visits sub-table).
        var visitRows = await scope.Database.FetchAsync<UuidCountRow>(
            "SELECT TicketUuid AS Uuid, COUNT(DISTINCT EventId) AS Cnt FROM TicketEventVisits " +
            "WHERE TicketType = @0 AND TicketUuid IS NOT NULL GROUP BY TicketUuid",
            new object[] { (int)TicketType.SeasonPass });
        var visits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in visitRows)
            if (v.Uuid is not null) visits[v.Uuid] = v.Cnt;

        return passes.Select(p => new SeasonPassListItem(
            Guid.TryParse(p.Uuid, out var g) ? g : Guid.Empty,
            ((TicketCategory)p.Category).DisplayName(),
            p.Price,
            ((TicketStatus)p.Status).ToString(),
            p.CreatedAt,
            visits.GetValueOrDefault(p.Uuid),
            BuyerName(p.BillingFirstName, p.BillingLastName),
            p.OrderNumber,
            p.OrderStatus is { } os ? PaymentState((OrderStatus)os) : null)).ToList();
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

    /// <summary>Projection for the pass+order join (mapped by column name/alias).</summary>
    public sealed class Row
    {
        public string Uuid { get; set; } = "";
        public int Category { get; set; }
        public decimal Price { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? OrderNumber { get; set; }
        public int? OrderStatus { get; set; }
        public string? BillingFirstName { get; set; }
        public string? BillingLastName { get; set; }
    }

    /// <summary>Projection for the visit-count query (mapped by column name/alias).</summary>
    public sealed class UuidCountRow
    {
        public string? Uuid { get; set; }
        public int Cnt { get; set; }
    }
}

/// <summary>Registers the Saisonkarten admin report (auto-discovered via <c>.AddComposers()</c>).</summary>
public sealed class SeasonPassAdminReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISeasonPassAdminReport, SeasonPassAdminReportReader>();
}
