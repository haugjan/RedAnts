namespace RedAnts.Features.Ticketing.Ports;

public enum PayrexxStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Declined
}

public sealed record PayrexxCreateRequest(
    int AmountInCents,
    string Currency,
    string Purpose,
    string ReferenceId,
    string SuccessUrl,
    string FailedUrl,
    string CancelUrl,
    string? Email,
    string? FirstName,
    string? LastName);

public sealed record PayrexxGatewayResult(string GatewayId, string Link);

public sealed record PayrexxRefundResult(bool Success, string? Error);

public interface IPayrexxGateway
{
    bool Enabled { get; }

    Task<PayrexxGatewayResult> CreateGatewayAsync(PayrexxCreateRequest request, CancellationToken cancellationToken = default);

    Task<PayrexxStatus> GetGatewayStatusAsync(string gatewayId, CancellationToken cancellationToken = default);

    Task<PayrexxRefundResult> RefundGatewayAsync(string gatewayId, CancellationToken cancellationToken = default);
}
