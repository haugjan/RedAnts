using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Orders;

public sealed record RefundSummary(int OrderId, decimal TotalGross, decimal RefundedConfirmed, decimal Reserved, decimal Remaining);

public interface IOrderRefunds
{
    Task<RefundSummary> GetSummaryAsync(int orderId);

    Task<OrderRefund> CreateAsync(int orderId, decimal amount, RefundMethod method, RefundStatus initialStatus,
        string? reference, string? reason, string? createdBy);

    Task ConfirmAsync(int refundId, string? payrexxRefundId, string? changedBy);

    Task FailAsync(int refundId, string? error);
}
