using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;

namespace RedAnts.Show.Features.Admin;

[Route("admin/show")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class ShowAdminController : Controller
{
    public static readonly IReadOnlyList<string> Pages = ["hallen", "musik", "decks", "bedienung", "vmix", "led"];

    [HttpGet("")]
    public IActionResult Index() => RedirectToAction(nameof(Page), new { page = Pages[0] });

    [HttpGet("{page}")]
    public IActionResult Page(string page)
    {
        var wanted = page.ToLowerInvariant();
        if (wanted == "musik") return Redirect("/admin/dj");
        return View("ShowAdmin", Pages.Contains(wanted) ? wanted : Pages[0]);
    }
}
