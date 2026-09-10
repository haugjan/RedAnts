using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.MemberCards;
using RedAnts.Ticketing.Features.Newsletter;
using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Features.SeasonPasses;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.Checkout;

public sealed class OrderFulfillment(
    IOrderRepository orders,
    IOrderLog orderLog,
    IEventTicketRepository tickets,
    ISeasonPassRepository passes,
    IConvertibleCards convertibleCards,
    IOrderAddOns orderAddOns,
    IAddOnNotifier addOnNotifier,
    ISeasonAddOnRepository seasonAddOns,
    IOrderMailer mailer,
    IPublicBaseUrl publicUrl,
    IOrderItems orderItems,
    INewsletterSignupRepository newsletter,
    IIssuedTicketReader issuedTickets,
    IUnitOfWork unitOfWork,
    CapacityReservation reservation,
    ILogger<OrderFulfillment> logger)
{
    private const string Channel = "Online-Kauf";

    private sealed record Issued(IReadOnlyList<OrderMailTicket> Tickets, IReadOnlyList<OrderAddOnLine> AddOns);

    public async Task<bool> FulfillAsync(int orderId)
    {
        var order = await orders.GetByIdAsync(orderId);
        if (order is null || order.Status != OrderStatus.Draft) return false;
        if (OrderSnapshot.Parse(order.FulfillmentPayload) is not { } snapshot) return false;

        var billing = order.BillingAddress;
        var buyer = billing.ToBuyer();
        var holderName = string.IsNullOrWhiteSpace(buyer.DisplayName) ? null : buyer.DisplayName;

        var issued = await unitOfWork.RunAsync<Issued?>(async () =>
        {
            if (!await orders.TryMarkPaidAsync(orderId)) return null;
            await orderLog.AppendAsync(order.Id, OrderStatus.Paid, Channel, "Online bezahlt");

            var mailTickets = new List<OrderMailTicket>();
            foreach (var item in snapshot.Items)
                for (var i = 0; i < item.Quantity; i++)
                    mailTickets.Add(item.IsSeasonPass
                        ? await IssuePassAsync(item, order, buyer, holderName)
                        : await IssueTicketAsync(item, order, buyer, holderName));

            await orders.CopyBillingToTicketsAsync(order.Id);
            return new Issued(mailTickets, await SaveAddOnsAsync(order, snapshot));
        });
        if (issued is null) return false;

        await reservation.ReleaseAsync(snapshot);
        await SaveOrderItemsAsync(order, snapshot);

        if (issued.AddOns.Count > 0)
            await addOnNotifier.NotifyAsync(order.OrderNumber, billing.FullName, billing.Email, issued.AddOns);

        var addOnInfos = await AddOnInfoTexts.CollectAsync(seasonAddOns, snapshot);
        await mailer.SendTicketsAsync(new OrderMailModel(
            order.OrderNumber, billing.Email, billing.FullName, order.TotalGross.Amount,
            publicUrl.Resolve(), issued.Tickets, addOnInfos));

        if (snapshot.SubscribeNewsletter)
            await newsletter.SubscribeAsync(billing.Email, billing.FullName, snapshot.NewsletterSource);

        return true;
    }

    private async Task<OrderMailTicket> IssuePassAsync(OrderSnapshotItem item, Order order, Buyer buyer, string? holderName)
    {
        var pass = await passes.SaveAsync(SeasonPass.Create(item.SeasonId, item.TierId, item.UnitPrice, order.Id, buyer, Channel));
        var category = await CategoryNameAsync(pass.Uuid, item.CategoryName);
        return new OrderMailTicket(TicketType.SeasonPass, pass.Uuid, item.SeasonId, item.EventName, category, holderName);
    }

    private async Task<OrderMailTicket> IssueTicketAsync(OrderSnapshotItem item, Order order, Buyer buyer, string? holderName)
    {
        var originType = item.OriginType is { } type ? (TicketType)type : (TicketType?)null;
        var originUuid = Guid.TryParse(item.OriginCardUuid, out var parsed) ? parsed : (Guid?)null;
        var ticket = await tickets.SaveAsync(EventTicket.Create(item.EventId, (TicketCategory)item.OriginCategory, item.UnitPrice, order.Id, buyer,
            Channel, tierId: item.TierId, originType: originType, originCardUuid: originUuid));
        if (originType == TicketType.SeasonSingle && originUuid is { } flexUuid)
            await convertibleCards.MarkFlexConvertedAsync(flexUuid, item.EventId);
        var category = await CategoryNameAsync(ticket.Uuid, item.CategoryName);
        return new OrderMailTicket(TicketType.EventTicket, ticket.Uuid, item.EventId, item.EventName, category, holderName);
    }

    private async Task<string> CategoryNameAsync(Guid uuid, string fallback)
    {
        var resolved = (await issuedTickets.FindAsync(uuid))?.CategoryName;
        return string.IsNullOrWhiteSpace(resolved) ? fallback : resolved;
    }

    private async Task<IReadOnlyList<OrderAddOnLine>> SaveAddOnsAsync(Order order, OrderSnapshot snapshot)
    {
        if (snapshot.AddOns.Count == 0) return [];
        var lines = snapshot.AddOns
            .Select(a => new OrderAddOnLine(a.SeasonId, a.EventName, default, a.CategoryName, a.Label, a.Price, a.Quantity, a.TierId))
            .ToList();
        await orderAddOns.SaveAsync(order.Id, lines);
        return lines;
    }

    private async Task SaveOrderItemsAsync(Order order, OrderSnapshot snapshot)
    {
        try
        {
            var lines = new List<OrderItem>();
            foreach (var item in snapshot.Items)
            {
                var kind = item.IsSeasonPass ? OrderItemKind.SeasonPass : OrderItemKind.EventTicket;
                var refId = item.IsSeasonPass ? item.SeasonId : item.EventId;
                var label = string.IsNullOrEmpty(item.CategoryName) ? item.EventName : $"{item.EventName} · {item.CategoryName}";
                lines.Add(OrderItem.Create(order.Id, kind, refId, default, label, item.Quantity, Money.Of(item.UnitPrice)));
            }
            foreach (var addOn in snapshot.AddOns)
                lines.Add(OrderItem.Create(order.Id, OrderItemKind.AddOn, addOn.SeasonId, default, addOn.Label, addOn.Quantity, Money.Of(addOn.Price)));
            if (lines.Count > 0)
                await orderItems.SaveAsync(order.Id, lines);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OrderItems für Bestellung {OrderNumber} konnten nicht gespeichert werden (per Backfill nachholbar)", order.OrderNumber);
        }
    }
}
