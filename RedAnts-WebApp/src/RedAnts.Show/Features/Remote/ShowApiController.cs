using Microsoft.AspNetCore.Mvc;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Board;

namespace RedAnts.Show.Features.Remote;

[ApiController]
[Route("api/show")]
public sealed class ShowApiController(
    DispatchShowCommand.Handler dispatch,
    GetShowProfiles.Handler profileQuery,
    GetBoardView.Handler viewQuery,
    RecordShowRoom.Handler roomRecorder,
    IConfiguration config) : ControllerBase
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
        var profiles = await profileQuery.HandleAsync(new GetShowProfiles.Query());
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

    [HttpGet("view")]
    public async Task<IActionResult> BoardView(string? room = null, long? since = null)
    {
        if (!KeyOk()) return Unauthorized();
        await roomRecorder.HandleAsync(new RecordShowRoom.Command(room));
        var published = await viewQuery.HandleAsync(new GetBoardView.Query(room, since), HttpContext.RequestAborted);
        return new JsonResult(new { connected = published is not null, version = published?.Version ?? 0, view = published?.View });
    }

    [HttpGet("press/{slot:int}")]
    public Task<IActionResult> Press(int slot, string? room = null) => Cmd(new ShowCommand("press", Room: room, Slot: slot));

    [HttpGet("play/{id}")]
    public Task<IActionResult> Play(string id, string? room = null) => Cmd(new ShowCommand("play", TileId: id, Room: room));

    [HttpGet("song/{id}/{index:int}")]
    public Task<IActionResult> Song(string id, int index, string? room = null) => Cmd(new ShowCommand("song", TileId: id, SongIndex: index, Room: room));

    [HttpGet("folder/{id}")]
    public Task<IActionResult> Folder(string id, string? room = null) => Cmd(new ShowCommand("folder", TileId: id, Room: room));

    [HttpGet("back")]
    public Task<IActionResult> Back(string? room = null) => Cmd(new ShowCommand("back", Room: room));

    [HttpGet("home")]
    public Task<IActionResult> Home(string? room = null) => Cmd(new ShowCommand("home", Room: room));

    [HttpGet("profile/{id}")]
    public Task<IActionResult> Profile(string id, string? room = null) => Cmd(new ShowCommand("profile", ProfileId: id, Room: room));

    [HttpGet("profile-next")]
    public Task<IActionResult> NextProfile(string? room = null) => Cmd(new ShowCommand("profile-next", Room: room));

    [HttpGet("profile-prev")]
    public Task<IActionResult> PreviousProfile(string? room = null) => Cmd(new ShowCommand("profile-prev", Room: room));

    [HttpGet("stop")]
    public Task<IActionResult> Stop(string? room = null) => Cmd(new ShowCommand("stop", Room: room));

    [HttpGet("pause")]
    public Task<IActionResult> Pause(string? room = null) => Cmd(new ShowCommand("pause", Room: room));

    [HttpGet("resume")]
    public Task<IActionResult> Resume(string? room = null) => Cmd(new ShowCommand("resume", Room: room));

    [HttpGet("fade")]
    public Task<IActionResult> Fade(string? room = null) => Cmd(new ShowCommand("fade", Room: room));

    [HttpPost("command")]
    public Task<IActionResult> Command([FromBody] ShowCommand cmd) => Cmd(cmd);

    private async Task<IActionResult> Cmd(ShowCommand cmd)
    {
        if (!KeyOk()) return Unauthorized();
        await roomRecorder.HandleAsync(new RecordShowRoom.Command(cmd.Room));
        var reached = await dispatch.HandleAsync(new DispatchShowCommand.Command(cmd));
        return Ok(new { ok = true, boards = reached });
    }
}
