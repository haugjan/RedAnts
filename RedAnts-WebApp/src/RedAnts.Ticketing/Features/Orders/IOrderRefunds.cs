using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Orders;

public sealed record RefundSummary(int OrderId, Money TotalGross, Money RefundedConfirmed, Money Reserved, Money Remaining);

public interface IOrderRefunds
{
    Task<RefundSummary> GetSummaryAsync(int orderId);

    Task<OrderRefund> CreateAsync(int orderId, Money amount, RefundMethod method, RefundStatus initialStatus,
        string? reference, string? reason, string? createdBy);

    Task ConfirmAsync(int refundId, string? payrexxRefundId, string? changedBy);

    Task FailAsync(int refundId, string? error);
}
