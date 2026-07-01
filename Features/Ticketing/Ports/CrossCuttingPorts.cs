using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Ports;

/// <summary>Spam protection on public POSTs (Cloudflare Turnstile).</summary>
public interface ICaptcha
{
    Task<bool> VerifyAsync(string? token, string? remoteIp);
}

/// <summary>Encodes integer entity ids into short opaque public ids (Sqids) and back.</summary>
public interface ISqidEncoder
{
    string Encode(int id);
    int? Decode(string sqid);
}

public sealed record PaymentRequest(
    decimal Amount,
    string Currency,
    string ReferenceId,
    string Purpose,
    string Email,
    string SuccessUrl,
    string FailedUrl,
    string CancelUrl);

public sealed record PaymentCreation(string PaymentUrl, string? GatewayId);

/// <summary>Online payment gateway (first adapter: Payrexx). Swappable behind this port.</summary>
public interface IPaymentGateway
{
    /// <summary>True when real gateway credentials are configured. When false, callers use a local dev simulation.</summary>
    bool IsConfigured { get; }
    Task<PaymentCreation> CreatePaymentAsync(PaymentRequest request);
}

/// <summary>Resolves the price of a season ticket from its category and age group.</summary>
public interface ISeasonTicketPricing
{
    decimal PriceFor(SeasonTicketCategory category, AgeGroup ageGroup);
    /// <summary>All configured prices, for display on the public purchase page.</summary>
    IReadOnlyList<(SeasonTicketCategory Category, AgeGroup AgeGroup, decimal Amount)> All();
}

/// <summary>Confirmation e-mails for ticket purchases (Brevo REST).</summary>
public interface ITicketEmail
{
    Task SendSingleTicketConfirmationAsync(SingleTicket ticket, Event evt);
    Task SendSeasonTicketConfirmationAsync(SeasonTicket ticket, Season season);
}
