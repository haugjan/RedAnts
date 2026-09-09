using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Ticketing.Features.Email;

public sealed class EmailTestController(
    IWebHostEnvironment environment,
    SendOrderMailSample.Handler orderMailSample,
    SendTestMail.Handler testMail) : Controller
{
    [HttpGet("/dev/ticket-mail-preview")]
    public async Task<IActionResult> TicketMailPreview(string? to = null)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var result = await orderMailSample.HandleAsync(new SendOrderMailSample.Command(to));
        return result.Sent is { } sent
            ? Content(sent ? $"Testmail an {to} gesendet." : "Versand fehlgeschlagen (siehe Logs).")
            : Content(result.Html, "text/html");
    }

    [HttpGet("/dev/test-mail")]
    public async Task<IActionResult> Send(string? to, Guid? uuid)
    {
        if (!environment.IsDevelopment()) return NotFound();
        if (string.IsNullOrWhiteSpace(to)) return BadRequest("Query parameter ?to=<email> is required.");

        var result = await testMail.HandleAsync(new SendTestMail.Command(to, uuid));
        return result.Success
            ? Content($"OK – Mail an {to} gesendet.")
            : StatusCode(502, $"Versand fehlgeschlagen: {result.Error}");
    }
}
