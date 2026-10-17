using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Show.Features.Matchday;

[Route("matchday")]
public sealed class MatchdayController : Controller
{
    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Index([FromQuery] string? halle) =>
        RedirectPermanent(halle is { Length: > 0 } hall ? $"/bord?halle={Uri.EscapeDataString(hall)}&spiel=1" : "/bord?spiel=1");
}
