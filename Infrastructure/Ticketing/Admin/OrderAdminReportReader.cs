using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class OrderAdminReportReader(IScopeProvider scopeProvider) : IOrderAdminReport
{
    public async Task<IReadOnlyList<OrderListItem>> GetBySeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var orders = await scope.Database.FetchAsync<OrderRow>(@"
            SELECT Id, OrderNumber, CreatedAt, Status, TotalGross,
                   BillingType, BillingFirstName, BillingLastName, BillingCompany,
                   BillingStreet, BillingAddressLine2, BillingPostalCode, BillingCity, BillingCountry, BillingEmail
            FROM Orders
            ORDER BY CreatedAt DESC");

        var eventTickets = await scope.Database.FetchAsync<ItemRow>(
            "SELECT OrderId, EventId AS RefId, Category FROM EventTickets WHERE OrderId IS NOT NULL");
        var seasonPasses = await scope.Database.FetchAsync<ItemRow>(
            "SELECT OrderId, SeasonId AS RefId, Category FROM SeasonPasses WHERE OrderId IS NOT NULL");

        var eventIdSet = new HashSet<int>(eventIds);

        var ticketsByOrder = eventTickets
            .Where(t => t.OrderId is not null && eventIdSet.Contains(t.RefId))
            .GroupBy(t => t.OrderId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var passesByOrder = seasonPasses
            .Where(p => p.OrderId is not null && p.RefId == seasonId)
            .GroupBy(p => p.OrderId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<OrderListItem>();
        foreach (var o in orders)
        {
            var tickets = ticketsByOrder.GetValueOrDefault(o.Id) ?? [];
            var passes = passesByOrder.GetValueOrDefault(o.Id) ?? [];
            if (tickets.Count == 0 && passes.Count == 0) continue;

            result.Add(new OrderListItem(
                o.OrderNumber,
                o.CreatedAt,
                (OrderStatus)o.Status,
                o.TotalGross,
                (BuyerType)(o.BillingType ?? 0),
                BuyerName(o),
                o.BillingStreet ?? "",
                o.BillingAddressLine2,
                o.BillingPostalCode ?? "",
                o.BillingCity ?? "",
                o.BillingCountry ?? "",
                o.BillingEmail ?? "",
                tickets.Count,
                Summarize(tickets),
                passes.Count,
                Summarize(passes)));
        }
        return result;
    }

    private static string BuyerName(OrderRow o)
    {
        if ((BuyerType)(o.BillingType ?? 0) == BuyerType.Company && !string.IsNullOrWhiteSpace(o.BillingCompany))
            return o.BillingCompany!;
        var name = $"{o.BillingFirstName} {o.BillingLastName}".Trim();
        return string.IsNullOrEmpty(name) ? "—" : name;
    }

    private static string Summarize(List<ItemRow> items)
    {
        if (items.Count == 0) return "—";
        return string.Join(" · ", items
            .GroupBy(i => (TicketCategory)i.Category)
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Count()}× {g.Key.DisplayName()}"));
    }

    public sealed class OrderRow
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int Status { get; set; }
        public decimal TotalGross { get; set; }
        public int? BillingType { get; set; }
        public string? BillingFirstName { get; set; }
        public string? BillingLastName { get; set; }
        public string? BillingCompany { get; set; }
        public string? BillingStreet { get; set; }
        public string? BillingAddressLine2 { get; set; }
        public string? BillingPostalCode { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingCountry { get; set; }
        public string? BillingEmail { get; set; }
    }

    public sealed class ItemRow
    {
        public int? OrderId { get; set; }
        public int RefId { get; set; }
        public int Category { get; set; }
    }
}

public sealed class OrderAdminReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderAdminReport, OrderAdminReportReader>();
}
