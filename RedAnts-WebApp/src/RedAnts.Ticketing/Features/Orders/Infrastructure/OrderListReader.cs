using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Orders.Infrastructure;

public sealed class OrderListReader(IScopeProvider scopeProvider) : IOrderListReader
{
    public async Task<IReadOnlyList<OrderListRow>> GetBySeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var orders = await scope.Database.FetchAsync<OrderRow>(@"
            SELECT Id, OrderNumber, CreatedAt, Status, TotalGross, PaymentMethod, PaymentSource,
                   BillingType, BillingFirstName, BillingLastName, BillingCompany,
                   BillingStreet, BillingAddressLine2, BillingPostalCode, BillingCity, BillingCountry, BillingEmail, FulfillmentPayload
            FROM Orders
            ORDER BY CreatedAt DESC");

        var eventTickets = await scope.Database.FetchAsync<ItemRow>(
            "SELECT OrderId, EventId AS RefId, Category, TierId FROM EventTickets WHERE OrderId IS NOT NULL");
        var seasonPasses = await scope.Database.FetchAsync<ItemRow>(
            "SELECT OrderId, SeasonId AS RefId, Category, TierId FROM SeasonPasses WHERE OrderId IS NOT NULL");
        var flexTickets = await scope.Database.FetchAsync<ItemRow>(
            "SELECT OrderId, SeasonId AS RefId, Category, TierId FROM SeasonSingleTickets WHERE OrderId IS NOT NULL");

        var tierNames = (await scope.Database.FetchAsync<TierNameRow>("SELECT Id, Name FROM SeasonPriceTiers"))
            .ToDictionary(t => t.Id, t => t.Name);

        var refundsByOrder = (await scope.Database.FetchAsync<RefundSumRow>(
                "SELECT OrderId, SUM(Amount) AS Amount FROM OrderRefunds WHERE Status = @0 GROUP BY OrderId",
                (int)RefundStatus.Confirmed))
            .ToDictionary(r => r.OrderId, r => r.Amount);

        var eventIdSet = new HashSet<int>(eventIds);

        var ticketsByOrder = eventTickets
            .Where(t => t.OrderId is not null && eventIdSet.Contains(t.RefId))
            .GroupBy(t => t.OrderId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var passesByOrder = seasonPasses
            .Where(p => p.OrderId is not null && p.RefId == seasonId)
            .GroupBy(p => p.OrderId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var flexByOrder = flexTickets
            .Where(f => f.OrderId is not null && f.RefId == seasonId)
            .GroupBy(f => f.OrderId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<OrderListRow>();
        foreach (var o in orders)
        {
            var tickets = ticketsByOrder.GetValueOrDefault(o.Id) ?? [];
            var passes = passesByOrder.GetValueOrDefault(o.Id) ?? [];
            var flex = flexByOrder.GetValueOrDefault(o.Id) ?? [];
            var planned = SnapshotItems(o.FulfillmentPayload, seasonId, eventIdSet);
            if (tickets.Count == 0 && passes.Count == 0 && flex.Count == 0 && planned.Count == 0) continue;

            result.Add(new OrderListRow(
                o.Id,
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
                tickets.Count > 0 ? tickets.Count : planned.EventTickets,
                tickets.Count > 0 ? Summarize(tickets, tierNames) : planned.EventSummary,
                passes.Count > 0 ? passes.Count : planned.Passes,
                passes.Count > 0 ? Summarize(passes, tierNames) : planned.PassSummary,
                flex.Count,
                Summarize(flex, tierNames),
                ResolvePaymentSource(o),
                refundsByOrder.GetValueOrDefault(o.Id)));
        }
        return result;
    }

    private sealed record PlannedItems(int EventTickets, string EventSummary, int Passes, string PassSummary)
    {
        public static readonly PlannedItems None = new(0, "—", 0, "—");
        public int Count => EventTickets + Passes;
    }

    private static PlannedItems SnapshotItems(string? payload, int seasonId, HashSet<int> eventIds)
    {
        if (OrderSnapshot.Parse(payload) is not { } snapshot) return PlannedItems.None;
        var events = snapshot.Items.Where(i => !i.IsSeasonPass && eventIds.Contains(i.EventId)).ToList();
        var passes = snapshot.Items.Where(i => i.IsSeasonPass && i.SeasonId == seasonId).ToList();
        return new PlannedItems(events.Sum(i => i.Quantity), SummarizePlanned(events), passes.Sum(i => i.Quantity), SummarizePlanned(passes));
    }

    private static string SummarizePlanned(List<OrderSnapshotItem> items) =>
        items.Count == 0
            ? "—"
            : string.Join(" · ", items
                .GroupBy(i => string.IsNullOrWhiteSpace(i.CategoryName) ? "Ticket" : i.CategoryName)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Sum(i => i.Quantity)}× {g.Key}"));

    private static PaymentSource? ResolvePaymentSource(OrderRow o)
    {
        if (o.PaymentSource is { } ps) return (PaymentSource)ps;
        return o.PaymentMethod == (int)PaymentMethod.Payrexx ? PaymentSource.Online : null;
    }

    private static string BuyerName(OrderRow o)
    {
        if ((BuyerType)(o.BillingType ?? 0) == BuyerType.Company && !string.IsNullOrWhiteSpace(o.BillingCompany))
            return o.BillingCompany!;
        var name = $"{o.BillingFirstName} {o.BillingLastName}".Trim();
        return string.IsNullOrEmpty(name) ? "—" : name;
    }

    private static string Summarize(List<ItemRow> items, IReadOnlyDictionary<int, string> tierNames)
    {
        if (items.Count == 0) return "—";
        return string.Join(" · ", items
            .GroupBy(i => i.TierId is { } tid && tierNames.TryGetValue(tid, out var name)
                ? name : ((TicketCategory)i.Category).DisplayName())
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Count()}× {g.Key}"));
    }

    public sealed class OrderRow
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
        public int Status { get; set; }
        public decimal TotalGross { get; set; }
        public int PaymentMethod { get; set; }
        public int? PaymentSource { get; set; }
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
        public string? FulfillmentPayload { get; set; }
    }

    public sealed class ItemRow
    {
        public int? OrderId { get; set; }
        public int RefId { get; set; }
        public int Category { get; set; }
        public int? TierId { get; set; }
    }

    public sealed class TierNameRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class RefundSumRow
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
    }
}
