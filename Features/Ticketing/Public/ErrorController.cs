using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Features.Ticketing.Public;

public sealed class ErrorController : Controller
{
    [HttpGet("/404")]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("~/Views/Error404.cshtml");
    }
}
