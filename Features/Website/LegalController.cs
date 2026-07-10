using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Features.Website;

public sealed class LegalController : Controller
{
    [HttpGet("/datenschutz")]
    public IActionResult Privacy() => View("~/Views/Datenschutz.cshtml");
}
