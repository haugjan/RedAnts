using Microsoft.AspNetCore.Mvc;
using RedAnts.Features.Ticketing.Purchase;

namespace RedAnts.Features.Ticketing.Public;

[ApiController]
[Route("api/tickets")]
public sealed class TicketPaymentController(CompletePurchase complete, ILogger<TicketPaymentController> logger) : ControllerBase
{
    /// <summary>Local development payment simulation (used when Payrexx is not configured):
    /// marks the ticket paid and redirects to the confirmation page.</summary>
    [HttpGet("sim-zahlung")]
    public async Task<IActionResult> SimulatePayment([FromQuery] Guid @ref, [FromQuery] string kind)
    {
        if (string.Equals(kind, "season", StringComparison.OrdinalIgnoreCase))
            await complete.CompleteSeasonAsync(@ref);
        else
            await complete.CompleteSingleAsync(@ref);

        return Redirect($"/tickets/bestaetigung/{@ref}");
    }

    /// <summary>Payrexx webhook: fires when a transaction status changes. We finalize the
    /// purchase when the transaction is confirmed. Always returns 200 so Payrexx stops retrying.</summary>
    [HttpPost("payrexx-webhook")]
    public async Task<IActionResult> PayrexxWebhook()
    {
        try
        {
            var form = await Request.ReadFormAsync();

            var status = form.FirstOrDefault(kv => kv.Key.EndsWith("[status]", StringComparison.OrdinalIgnoreCase)).Value.ToString();
            var referenceRaw = form.FirstOrDefault(kv => kv.Key.EndsWith("[referenceId]", StringComparison.OrdinalIgnoreCase)).Value.ToString();

            if (string.Equals(status, "confirmed", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(referenceRaw, out var reference))
            {
                if (!await complete.CompleteSingleAsync(reference))
                    await complete.CompleteSeasonAsync(reference);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payrexx webhook processing failed.");
        }

        return Ok();
    }
}
