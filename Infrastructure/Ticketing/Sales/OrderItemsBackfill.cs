using Microsoft.Extensions.Logging;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class OrderItemsBackfillComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, OrderItemsBackfill>();
}

public sealed class OrderItemsBackfill(
    IScopeProvider scopeProvider, ISeasons seasons, IEvents events, ILogger<OrderItemsBackfill> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var pending = await db.FetchAsync<int>(@"
SELECT DISTINCT o.OrderId FROM (
    SELECT OrderId FROM EventTickets WHERE OrderId IS NOT NULL
    UNION ALL SELECT OrderId FROM SeasonSingleTickets WHERE OrderId IS NOT NULL
    UNION ALL SELECT OrderId FROM SeasonPasses WHERE OrderId IS NOT NULL
    UNION ALL SELECT OrderId FROM MembershipCards WHERE OrderId IS NOT NULL
    UNION ALL SELECT OrderId FROM OrderAddOns
) o
LEFT JOIN OrderItems i ON i.OrderId = o.OrderId
WHERE i.OrderId IS NULL");
        if (pending.Count == 0) return;

        var eventNames = (await events.GetAllAsync()).GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.First().Name);
        var seasonNames = (await seasons.GetAllAsync()).GroupBy(s => s.Id).ToDictionary(g => g.Key, g => g.First().Name);

        var filled = 0;
        foreach (var orderId in pending)
        {
            try
            {
                var items = new List<OrderItem>();

                foreach (var g in await GroupTicketsAsync(db, "EventTickets", "EventId", orderId))
                    items.Add(OrderItem.Create(orderId, OrderItemKind.EventTicket, g.RefId, (TicketCategory)g.Category,
                        TicketLabel(eventNames, g.RefId, (TicketCategory)g.Category), g.Qty, g.Price));

                foreach (var g in await GroupTicketsAsync(db, "SeasonSingleTickets", "SeasonId", orderId))
                    items.Add(OrderItem.Create(orderId, OrderItemKind.SeasonSingle, g.RefId, (TicketCategory)g.Category,
                        TicketLabel(seasonNames, g.RefId, (TicketCategory)g.Category), g.Qty, g.Price));

                foreach (var g in await GroupTicketsAsync(db, "SeasonPasses", "SeasonId", orderId))
                    items.Add(OrderItem.Create(orderId, OrderItemKind.SeasonPass, g.RefId, (TicketCategory)g.Category,
                        TicketLabel(seasonNames, g.RefId, (TicketCategory)g.Category), g.Qty, g.Price));

                foreach (var g in await GroupCardsAsync(db, orderId))
                    items.Add(OrderItem.Create(orderId, OrderItemKind.MemberCard, g.RefId, default,
                        CardLabel(seasonNames, g.RefId, (MemberCategory)g.Category), g.Qty, 0m));

                var addOns = await db.FetchAsync<AddOnRow>(
                    "SELECT SeasonId AS RefId, Category, Label, Price, Quantity AS Qty FROM OrderAddOns WHERE OrderId = @0", orderId);
                foreach (var a in addOns.Where(a => a.Qty >= 1))
                    items.Add(OrderItem.Create(orderId, OrderItemKind.AddOn, a.RefId, (TicketCategory)a.Category,
                        a.Label, a.Qty, a.Price));

                if (items.Count == 0) continue;

                foreach (var item in items)
                    await db.InsertAsync(new OrderItemRecord
                    {
                        OrderId = orderId,
                        Kind = (int)item.Kind,
                        ArticleGuid = item.ArticleGuid,
                        RefId = item.RefId,
                        Category = (int)item.Category,
                        Label = item.Label,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                filled++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "OrderItems-Backfill für Bestellung {OrderId} übersprungen", orderId);
            }
        }

        if (filled > 0) logger.LogInformation("OrderItems-Backfill: {Count} Bestellungen nachgezogen", filled);
    }

    private static async Task<List<GroupRow>> GroupTicketsAsync(IDatabase db, string table, string refColumn, int orderId) =>
        await db.FetchAsync<GroupRow>(
            $"SELECT {refColumn} AS RefId, Category, Price, COUNT(*) AS Qty FROM {table} " +
            $"WHERE OrderId = @0 GROUP BY {refColumn}, Category, Price", orderId);

    private static async Task<List<GroupRow>> GroupCardsAsync(IDatabase db, int orderId) =>
        await db.FetchAsync<GroupRow>(
            "SELECT SeasonId AS RefId, Category, CAST(0 AS decimal(18,2)) AS Price, COUNT(*) AS Qty FROM MembershipCards " +
            "WHERE OrderId = @0 GROUP BY SeasonId, Category", orderId);

    private static string TicketLabel(IReadOnlyDictionary<int, string> names, int refId, TicketCategory category)
    {
        var cat = category.DisplayName();
        return names.TryGetValue(refId, out var name) && !string.IsNullOrWhiteSpace(name) ? $"{name} · {cat}" : cat;
    }

    private static string CardLabel(IReadOnlyDictionary<int, string> names, int refId, MemberCategory category)
    {
        var cat = category.DisplayName();
        return names.TryGetValue(refId, out var name) && !string.IsNullOrWhiteSpace(name) ? $"{name} · {cat}" : cat;
    }

    private sealed class GroupRow
    {
        public int RefId { get; set; }
        public int Category { get; set; }
        public decimal Price { get; set; }
        public int Qty { get; set; }
    }

    private sealed class AddOnRow
    {
        public int RefId { get; set; }
        public int Category { get; set; }
        public string Label { get; set; } = "";
        public decimal Price { get; set; }
        public int Qty { get; set; }
    }
}
