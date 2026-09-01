using Microsoft.AspNetCore.Mvc;
using RedAnts.Infrastructure.Show;

namespace RedAnts.Features.Show;

[ApiController]
[Route("api/show")]
public sealed class ShowApiController(IShowRemote remote, IShowProfileStore store, IConfiguration config) : ControllerBase
{
    private bool KeyOk()
    {
        var key = config["Show:ApiKey"] ?? config["Show:BoardPassword"];
        if (string.IsNullOrEmpty(key)) return true;
        var provided = Request.Query["key"].ToString();
        if (string.IsNullOrEmpty(provided)) provided = Request.Headers["X-Show-Key"].ToString();
        return string.Equals(provided, key, StringComparison.Ordinal);
    }

    [HttpGet("state")]
    public async Task<IActionResult> State()
    {
        if (!KeyOk()) return Unauthorized();
        var profiles = await store.GetAllAsync();
        var dto = profiles.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            color = p.Color,
            tiles = p.Root.Where(b => !b.Panic).Select(MapTile).ToList(),
        });
        return new JsonResult(new { profiles = dto });
    }

    private static object MapTile(ShowButton b) => new
    {
        id = b.Id,
        label = b.Label,
        icon = b.Icon,
        color = b.Color,
        subtitle = b.Subtitle,
        folder = b.IsFolder,
        songs = b.EffectiveSongs.Count(s => !string.IsNullOrWhiteSpace(s.Ref)),
        children = b.IsFolder ? b.Children!.Select(MapTile).ToList() : null,
    };

    [HttpGet("play/{id}")]
    public Task<IActionResult> Play(string id) => Cmd(new ShowCommand("play", TileId: id));

    [HttpGet("song/{id}/{index:int}")]
    public Task<IActionResult> Song(string id, int index) => Cmd(new ShowCommand("song", TileId: id, SongIndex: index));

    [HttpGet("folder/{id}")]
    public Task<IActionResult> Folder(string id) => Cmd(new ShowCommand("folder", TileId: id));

    [HttpGet("back")]
    public Task<IActionResult> Back() => Cmd(new ShowCommand("back"));

    [HttpGet("home")]
    public Task<IActionResult> Home() => Cmd(new ShowCommand("home"));

    [HttpGet("profile/{id}")]
    public Task<IActionResult> Profile(string id) => Cmd(new ShowCommand("profile", ProfileId: id));

    [HttpGet("stop")]
    public Task<IActionResult> Stop() => Cmd(new ShowCommand("stop"));

    [HttpGet("pause")]
    public Task<IActionResult> Pause() => Cmd(new ShowCommand("pause"));

    [HttpGet("resume")]
    public Task<IActionResult> Resume() => Cmd(new ShowCommand("resume"));

    [HttpGet("fade")]
    public Task<IActionResult> Fade() => Cmd(new ShowCommand("fade"));

    [HttpPost("command")]
    public Task<IActionResult> Command([FromBody] ShowCommand cmd) => Cmd(cmd);

    private async Task<IActionResult> Cmd(ShowCommand cmd)
    {
        if (!KeyOk()) return Unauthorized();
        var reached = await remote.DispatchAsync(cmd);
        return Ok(new { ok = true, boards = reached });
    }
}
