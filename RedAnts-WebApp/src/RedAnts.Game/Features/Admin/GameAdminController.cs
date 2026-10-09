using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;

namespace RedAnts.Game.Features.Admin;

[Route("admin/game")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class GameAdminController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("GameAdmin");
}
