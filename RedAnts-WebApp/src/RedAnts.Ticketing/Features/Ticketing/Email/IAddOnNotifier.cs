using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Email;

public interface IAddOnNotifier
{
    Task NotifyAsync(string orderNumber, string buyerName, string buyerEmail,
        IReadOnlyList<OrderAddOnLine> lines, CancellationToken cancellationToken = default);
}
