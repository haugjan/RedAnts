using Microsoft.AspNetCore.Mvc;

namespace RedAnts.DJ.Features.Board;

[Route("dj")]
public sealed class DJController : Controller
{
    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Index() => View("Index");
}
