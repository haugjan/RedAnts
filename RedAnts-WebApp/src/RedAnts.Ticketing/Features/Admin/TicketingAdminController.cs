using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Umbraco.Cms.Core;

namespace RedAnts.Ticketing.Features.Admin;

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
        return View("~/Features/Admin/Views/Admin.cshtml", new AdminIdentity(name, email, isAdmin));
    }
}
