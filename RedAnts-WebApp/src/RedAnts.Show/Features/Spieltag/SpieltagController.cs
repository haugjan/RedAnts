using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Show.Features.Spieltag;

[Route("spieltag")]
public sealed class SpieltagController : Controller
{
    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Index() => View("Spieltag");
}
