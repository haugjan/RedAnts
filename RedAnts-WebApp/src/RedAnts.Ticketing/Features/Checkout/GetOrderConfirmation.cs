using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.Checkout;

public sealed record ConfirmationTicket(Guid Uuid, string EventName, string CategoryName, string Token, int Type = 0,
    string? DateText = null, string? VenueName = null, string? HolderName = null);

public static class GetOrderConfirmation
{
    public sealed record Query(int OrderId);

    public sealed record Result(int OrderId, string OrderNumber, string Email, decimal Total, bool Paid, bool IsQuickBuy,
        IReadOnlyList<ConfirmationTicket> Tickets, IReadOnlyList<string> AddOnInfoTexts);

    public sealed class Handler(IOrderRepository orders, IOrderConfirmationReader confirmation, ITicketTokens tokens, IIssuedTicketReader issuedTickets,
        IEventReader events, ISeasonReader seasons, IVenueReader venues, ISeasonAddOnRepository seasonAddOns)
    {
        public async Task<Result?> HandleAsync(Query query)
        {
            var order = await orders.GetByIdAsync(query.OrderId);
            if (order is null) return null;

            var snapshot = OrderSnapshot.Parse(order.FulfillmentPayload);
            var paid = order.Status == OrderStatus.Paid;
            var confirmationTickets = paid ? await TicketsAsync(order, snapshot) : [];
            var addOnInfos = paid && snapshot is not null ? await AddOnInfoTexts.CollectAsync(seasonAddOns, snapshot) : [];
            return new Result(order.Id, order.OrderNumber, order.BillingAddress.Email.Value, order.TotalGross.Amount, paid,
                snapshot?.IsQuickBuy ?? false, confirmationTickets, addOnInfos);
        }

        private async Task<List<ConfirmationTicket>> TicketsAsync(Order order, OrderSnapshot? snapshot)
        {
            var names = new Dictionary<(int Kind, int RefId, int TierId), (string EventName, string CategoryName)>();
            foreach (var item in snapshot?.Items ?? [])
                names[(item.Kind, item.IsSeasonPass ? item.SeasonId : item.EventId, item.TierId)] = (item.EventName, item.CategoryName);

            var displayName = order.BillingAddress.ToBuyer().DisplayName;
            var holderName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;

            var result = new List<ConfirmationTicket>();
            foreach (var ticket in await confirmation.GetEventTicketsAsync(order.Id))
            {
                names.TryGetValue(((int)CartLineKind.EventTicket, ticket.EventId, ticket.TierId ?? 0), out var known);
                result.Add(new ConfirmationTicket(ticket.Uuid, known.EventName ?? "", await CategoryNameAsync(ticket.Uuid, known.CategoryName ?? ""),
                    tokens.CreateShort(ticket.Uuid), (int)TicketType.EventTicket, await EventDateTextAsync(ticket.EventId),
                    await EventVenueNameAsync(ticket.EventId), holderName));
            }
            foreach (var pass in await confirmation.GetSeasonPassesAsync(order.Id))
            {
                names.TryGetValue(((int)CartLineKind.SeasonPass, pass.SeasonId, pass.TierId ?? 0), out var known);
                result.Add(new ConfirmationTicket(pass.Uuid, known.EventName ?? "", await CategoryNameAsync(pass.Uuid, known.CategoryName ?? ""),
                    tokens.CreateShort(pass.Uuid), (int)TicketType.SeasonPass, await SeasonDateTextAsync(pass.SeasonId), HolderName: holderName));
            }
            return result;
        }

        private async Task<string> CategoryNameAsync(Guid uuid, string fallback)
        {
            var resolved = (await issuedTickets.FindAsync(uuid))?.CategoryName;
            return string.IsNullOrWhiteSpace(resolved) ? fallback : resolved;
        }

        private async Task<string?> EventDateTextAsync(int eventId) =>
            await events.FindByIdAsync(eventId) is { } evt
                ? evt.TimeUnknown ? $"{evt.Date:dd.MM.yyyy}" : $"{evt.Date:dd.MM.yyyy}, {evt.StartTime:HH:mm} Uhr"
                : null;

        private async Task<string?> EventVenueNameAsync(int eventId) =>
            await events.FindByIdAsync(eventId) is { VenueId: > 0 } evt
                ? (await venues.FindByIdAsync(evt.VenueId))?.Name
                : null;

        private async Task<string?> SeasonDateTextAsync(int seasonId) =>
            await seasons.FindByIdAsync(seasonId) is { } season
                ? $"{season.StartDate:dd.MM.yyyy} – {season.EndDate:dd.MM.yyyy}"
                : null;
    }
}
