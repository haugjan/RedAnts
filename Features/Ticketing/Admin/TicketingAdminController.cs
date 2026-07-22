using System.Security.Claims;
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
        var name = User.Identity?.Name ?? "admin";
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        var isAdmin = User.IsInRole(Constants.Security.AdminGroupAlias);
        return View("~/Features/Ticketing/Admin/Views/Admin.cshtml", new AdminIdentity(name, email, isAdmin));
    }
}
