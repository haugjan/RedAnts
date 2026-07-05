using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Email;

public sealed record OrderMailTicket(TicketType Type, Guid Uuid, int ScopeId, string EventName, string CategoryName);

public sealed record OrderMailModel(
    string OrderNumber,
    string ToEmail,
    string ToName,
    decimal Total,
    string BaseUrl,
    IReadOnlyList<OrderMailTicket> Tickets);

public interface IOrderMailer
{
    Task<bool> SendTicketsAsync(OrderMailModel model, CancellationToken cancellationToken = default);
}
