using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Show.Features.Matchday;

[Route("matchday")]
public sealed class MatchdayController : Controller
{
    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Index() => View("Matchday");
}
