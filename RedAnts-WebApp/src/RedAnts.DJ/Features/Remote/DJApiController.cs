using Microsoft.AspNetCore.Mvc;
using RedAnts.DJ.Domain;
using RedAnts.DJ.Features.Board;

namespace RedAnts.DJ.Features.Remote;

[ApiController]
[Route("api/dj")]
public sealed class DJApiController(
    DispatchDJCommand.Handler dispatch,
    GetDJProfiles.Handler profileQuery,
    GetBoardView.Handler viewQuery,
    RecordDJRoom.Handler roomRecorder,
    IConfiguration config) : ControllerBase
{
    private bool KeyOk()
    {
        var key = config["Show:ApiKey"] ?? config["Show:BoardPassword"];
        if (string.IsNullOrEmpty(key)) return true;
        var provided = Request.Query["key"].ToString();
        if (string.IsNullOrEmpty(provided)) provided = Request.Headers["X-DJ-Key"].ToString();
        return string.Equals(provided, key, StringComparison.Ordinal);
    }

    [HttpGet("state")]
    public async Task<IActionResult> State()
    {
        if (!KeyOk()) return Unauthorized();
        var profiles = await profileQuery.HandleAsync(new GetDJProfiles.Query());
        var dto = profiles.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            color = p.Color,
            tiles = p.Root.Where(b => !b.Panic).Select(MapTile).ToList(),
        });
        return new JsonResult(new { profiles = dto });
    }

    private static object MapTile(DJButton b) => new
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

    [HttpGet("view")]
    public async Task<IActionResult> BoardView(string? room = null, long? since = null)
    {
        if (!KeyOk()) return Unauthorized();
        await roomRecorder.HandleAsync(new RecordDJRoom.Command(room));
        var published = await viewQuery.HandleAsync(new GetBoardView.Query(room, since), HttpContext.RequestAborted);
        return new JsonResult(new { connected = published is not null, version = published?.Version ?? 0, view = published?.View });
    }

    [HttpGet("press/{slot:int}")]
    public Task<IActionResult> Press(int slot, string? room = null) => Cmd(new DJCommand("press", Room: room, Slot: slot));

    [HttpGet("play/{id}")]
    public Task<IActionResult> Play(string id, string? room = null) => Cmd(new DJCommand("play", TileId: id, Room: room));

    [HttpGet("song/{id}/{index:int}")]
    public Task<IActionResult> Song(string id, int index, string? room = null) => Cmd(new DJCommand("song", TileId: id, SongIndex: index, Room: room));

    [HttpGet("folder/{id}")]
    public Task<IActionResult> Folder(string id, string? room = null) => Cmd(new DJCommand("folder", TileId: id, Room: room));

    [HttpGet("back")]
    public Task<IActionResult> Back(string? room = null) => Cmd(new DJCommand("back", Room: room));

    [HttpGet("home")]
    public Task<IActionResult> Home(string? room = null) => Cmd(new DJCommand("home", Room: room));

    [HttpGet("profile/{id}")]
    public Task<IActionResult> Profile(string id, string? room = null) => Cmd(new DJCommand("profile", ProfileId: id, Room: room));

    [HttpGet("profile-next")]
    public Task<IActionResult> NextProfile(string? room = null) => Cmd(new DJCommand("profile-next", Room: room));

    [HttpGet("profile-prev")]
    public Task<IActionResult> PreviousProfile(string? room = null) => Cmd(new DJCommand("profile-prev", Room: room));

    [HttpGet("stop")]
    public Task<IActionResult> Stop(string? room = null) => Cmd(new DJCommand("stop", Room: room));

    [HttpGet("pause")]
    public Task<IActionResult> Pause(string? room = null) => Cmd(new DJCommand("pause", Room: room));

    [HttpGet("resume")]
    public Task<IActionResult> Resume(string? room = null) => Cmd(new DJCommand("resume", Room: room));

    [HttpGet("fade")]
    public Task<IActionResult> Fade(string? room = null) => Cmd(new DJCommand("fade", Room: room));

    [HttpGet("previous")]
    public Task<IActionResult> Previous(string? room = null) => Cmd(new DJCommand("previous", Room: room));

    [HttpGet("next")]
    public Task<IActionResult> Next(string? room = null) => Cmd(new DJCommand("next", Room: room));

    [HttpPost("command")]
    public Task<IActionResult> Command([FromBody] DJCommand cmd) => Cmd(cmd);

    private async Task<IActionResult> Cmd(DJCommand cmd)
    {
        if (!KeyOk()) return Unauthorized();
        await roomRecorder.HandleAsync(new RecordDJRoom.Command(cmd.Room));
        var reached = await dispatch.HandleAsync(new DispatchDJCommand.Command(cmd));
        return Ok(new { ok = true, boards = reached });
    }
}
