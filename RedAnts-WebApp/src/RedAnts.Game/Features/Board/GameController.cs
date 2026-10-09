using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Game.Features.Board;

[Route("game")]
public sealed class GameController : Controller
{
    private const string ManagerCookie = "RedAnts.Game.Manager";
    private const int TokenLength = 32;

    [HttpGet("")]
    public IActionResult Index()
    {
        var token = Request.Cookies[ManagerCookie];
        if (token is null || token.Length != TokenLength || !token.All(char.IsAsciiLetterOrDigit))
        {
            token = Guid.NewGuid().ToString("N");
            Response.Cookies.Append(ManagerCookie, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(365),
                IsEssential = true,
            });
        }

        return View("GameBoard", token);
    }
}
