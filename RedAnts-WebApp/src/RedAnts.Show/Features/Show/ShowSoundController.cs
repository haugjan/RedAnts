using Microsoft.AspNetCore.Mvc;
using RedAnts.Features.Show.Ports;

namespace RedAnts.Features.Show;

[Route("show/sound")]
public sealed class ShowSoundController(IShowSoundUploader sounds) : Controller
{
    [HttpGet("{**path}")]
    public Task<IActionResult> Get(string? path) => this.StreamShowSoundAsync(sounds, path);
}
