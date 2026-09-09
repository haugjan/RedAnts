using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Show.Features.Sounds;

[Route("show/sound")]
public sealed class ShowSoundController(IShowSoundUploader sounds) : Controller
{
    [HttpGet("{**path}")]
    public Task<IActionResult> Get(string? path) => this.StreamShowSoundAsync(sounds, path);
}
