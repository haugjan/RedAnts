using Microsoft.AspNetCore.Mvc;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Board;

[Route("bord")]
public sealed class ShowBoardController : Controller
{
    [HttpGet("hallen")]
    public async Task<IActionResult> Halls([FromServices] GetHalls.Handler halls)
    {
        var rows = await halls.HandleAsync(new GetHalls.Query());
        return Json(rows.Select(row => new { id = row.Id, name = row.Name, agent = row.AgentConnected }));
    }

    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Index() => View("Board");
}
