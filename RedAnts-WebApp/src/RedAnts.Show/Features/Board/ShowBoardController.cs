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

    [HttpGet("hallen/{id}/agent")]
    public async Task<IActionResult> AgentAccess(string id, [FromServices] GetHall.Handler halls)
    {
        var hall = await halls.HandleAsync(new GetHall.Query(id.ToLowerInvariant()));
        if (hall is null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        return Json(new { id = hall.Id, name = hall.Name, key = hall.AgentKey });
    }

    [HttpGet("")]
    [HttpGet("{**path}")]
    public IActionResult Index() => View("Board");
}
