using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Admin;

public sealed record RefundInput(
    int OrderId,
    decimal Amount,
    RefundMethod Method,
    bool ViaPayrexx,
    string? Reference,
    string? Reason,
    bool DeactivateTickets,
    string? ChangedBy);

public sealed record RefundOutcome(
    string RefundNumber,
    decimal RefundedTotal,
    decimal Remaining,
    OrderStatus Status,
    int DeactivatedTickets);

public interface IOrderRefundService
{
    Task<RefundSummary> GetSummaryAsync(int orderId);

    Task<IReadOnlyList<OrderRefund>> GetByOrderAsync(int orderId);

    Task<RefundOutcome> RefundAsync(RefundInput input);
}
