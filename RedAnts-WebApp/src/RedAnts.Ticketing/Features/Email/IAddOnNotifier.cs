using RedAnts.Ticketing.Features.Orders;

namespace RedAnts.Ticketing.Features.Email;

public interface IAddOnNotifier
{
    Task NotifyAsync(string orderNumber, string buyerName, string buyerEmail,
        IReadOnlyList<OrderAddOnLine> lines, CancellationToken cancellationToken = default);
}
