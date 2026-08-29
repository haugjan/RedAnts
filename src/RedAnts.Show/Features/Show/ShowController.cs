using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Features.Show;

[Route("show")]
public sealed class ShowController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Features/Show/Views/Index.cshtml");
}
