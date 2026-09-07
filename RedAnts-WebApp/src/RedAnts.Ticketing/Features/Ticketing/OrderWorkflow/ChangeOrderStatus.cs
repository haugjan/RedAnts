using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.OrderWorkflow;

public static class ChangeOrderStatus
{
    public sealed record Command(int OrderId, OrderStatus Target, string? ChangedBy);

    public sealed record Result(bool Changed, int DeactivatedTickets);

    public sealed class Handler(IOrders orders, IOrderLog log, IOrderTickets orderTickets)
    {
        public async Task<Result> HandleAsync(Command command)
        {
            var order = await orders.GetByIdAsync(command.OrderId)
                ?? throw new DomainException("Bestellung wurde nicht gefunden.");
            if (!order.ChangeStatus(command.Target)) return new Result(false, 0);

            await orders.SaveAsync(order);
            var deactivated = order.DeactivatesTickets ? await orderTickets.DeactivateByOrderAsync(order.Id) : 0;
            await log.AppendAsync(order.Id, command.Target, command.ChangedBy, Note(deactivated));
            return new Result(true, deactivated);
        }

        private static string Note(int deactivated) =>
            deactivated > 0 ? $"Admin-Änderung · {deactivated} Ticket(s) deaktiviert" : "Admin-Änderung";
    }
}
