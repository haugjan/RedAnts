using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Show.Features.Music;

[Route("dj")]
public sealed class DJController : Controller
{
    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Index() => View("Index");
}
