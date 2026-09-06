using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Show;

[Route("admin/show")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class ShowAdminController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Features/Show/Views/Admin.cshtml");
}
