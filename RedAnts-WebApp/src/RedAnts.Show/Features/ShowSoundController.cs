using Microsoft.AspNetCore.Mvc;
using RedAnts.Show.Features.Ports;

namespace RedAnts.Show.Features;

[Route("show/sound")]
public sealed class ShowSoundController(IShowSoundUploader sounds) : Controller
{
    [HttpGet("{**path}")]
    public Task<IActionResult> Get(string? path) => this.StreamShowSoundAsync(sounds, path);
}
