using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Email;

public sealed record OrderMailTicket(TicketType Type, Guid Uuid, int ScopeId, string EventName, string CategoryName, string? HolderName = null);

public sealed record OrderMailModel(
    string OrderNumber,
    string ToEmail,
    string ToName,
    decimal Total,
    string BaseUrl,
    IReadOnlyList<OrderMailTicket> Tickets,
    IReadOnlyList<string>? AddOnInfoTexts = null);

public interface IOrderMailer
{
    Task<bool> SendTicketsAsync(OrderMailModel model, CancellationToken cancellationToken = default);
    Task<string> RenderAsync(OrderMailModel model);
}
