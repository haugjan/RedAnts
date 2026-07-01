using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Purchase;

/// <summary>Builds the URL the buyer is redirected to: a Payrexx hosted page when configured,
/// otherwise a local payment-simulation endpoint so the flow is testable in development.</summary>
internal static class PaymentRedirect
{
    public static async Task<string> BuildAsync(
        IPaymentGateway gateway, string baseUrl, Guid ticketRef, TicketKind kind,
        decimal amount, string email, string purpose)
    {
        var b = baseUrl.TrimEnd('/');
        var confirmationUrl = $"{b}/tickets/bestaetigung/{ticketRef}";

        if (!gateway.IsConfigured)
            return $"{b}/api/tickets/sim-zahlung?ref={ticketRef}&kind={kind.ToString().ToLowerInvariant()}";

        var creation = await gateway.CreatePaymentAsync(new PaymentRequest(
            Amount: amount,
            Currency: "CHF",
            ReferenceId: ticketRef.ToString(),
            Purpose: purpose,
            Email: email,
            SuccessUrl: confirmationUrl,
            FailedUrl: $"{b}/tickets",
            CancelUrl: $"{b}/tickets"));

        return creation.PaymentUrl;
    }
}
