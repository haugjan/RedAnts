using Microsoft.AspNetCore.Mvc;

namespace RedAnts.DJ.Features.Sounds;

[Route("dj/sound")]
public sealed class DJSoundController(OpenDJSound.Handler sounds) : Controller
{
    [HttpGet("{**path}")]
    public Task<IActionResult> Get(string? path) => this.StreamDJSoundAsync(sounds, path);
}
