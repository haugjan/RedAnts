using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Show.Features.Board;

[Route("show")]
public sealed class ShowController : Controller
{
    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Index() => View("Index");
}
