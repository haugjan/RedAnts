using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Ticketing.Features.Admission;

public sealed class ScanController : Controller
{
    [HttpGet("/scan")]
    public IActionResult Index() => View("ScanTickets", new ScanSession(
        HttpContext.Items["HelperName"] as string ?? "",
        HttpContext.Items["HelperAllEvents"] as bool? ?? true,
        HttpContext.Items["HelperEventIds"] as string ?? "",
        HttpContext.Items["HelperCanRebook"] as bool? ?? false));
}

public sealed record ScanSession(string HelperName, bool AllEvents, string AllowedEventIds, bool CanRebook);
