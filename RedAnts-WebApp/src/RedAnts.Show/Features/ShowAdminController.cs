using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Show.Features.Ports;
using Umbraco.Cms.Core;

namespace RedAnts.Show.Features;

[Route("admin/show")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class ShowAdminController(IShowSoundUploader sounds) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Features/Views/Admin.cshtml");

    [HttpGet("sound/{**path}")]
    public Task<IActionResult> Sound(string? path) => this.StreamShowSoundAsync(sounds, path);
}
