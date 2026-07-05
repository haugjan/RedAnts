using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Admin;

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
