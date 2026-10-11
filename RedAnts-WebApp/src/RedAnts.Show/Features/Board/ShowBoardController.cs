using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Show.Features.Board;

[Route("bord")]
public sealed class ShowBoardController : Controller
{
    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Index() => View("Board");
}
