using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>
/// Iframe target for the Ticketing backoffice dashboard. Serves the HTML page that mounts the
/// Blazor Server admin app. Guarded by the backoffice auth scheme so only logged-in backoffice
/// users can reach it (the page runs inside the backoffice iframe, sharing its auth cookie).
/// </summary>
[Route("admin/ticketing")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class TicketingAdminController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var adminUser = User.Identity?.Name ?? "admin";
        return View("~/Features/Ticketing/Admin/Views/Admin.cshtml", adminUser);
    }
}
