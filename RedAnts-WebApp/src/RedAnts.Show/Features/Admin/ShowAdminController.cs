using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Show.Features.Sounds;
using Umbraco.Cms.Core;

namespace RedAnts.Show.Features.Admin;

[Route("admin/show")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class ShowAdminController(
    OpenShowSound.Handler sounds,
    StartSpotifyConnect.Handler spotifyConnect,
    CompleteSpotifyConnect.Handler spotifyComplete,
    GetSpotifyAccessToken.Handler spotifyToken) : Controller
{
    private const string StateCookie = "show_spotify_state";

    [HttpGet("")]
    public IActionResult Index() => View("ShowAdmin");

    [HttpGet("sound/{**path}")]
    public Task<IActionResult> Sound(string? path) => this.StreamShowSoundAsync(sounds, path);

    [HttpGet("spotify/connect")]
    public async Task<IActionResult> SpotifyConnect()
    {
        var state = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(StateCookie, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10),
        });
        return Redirect(await spotifyConnect.HandleAsync(new StartSpotifyConnect.Query(RedirectUri(), state)));
    }

    [HttpGet("spotify/callback")]
    public async Task<IActionResult> SpotifyCallback(string? code, string? state, string? error)
    {
        var expected = Request.Cookies[StateCookie];
        Response.Cookies.Delete(StateCookie);

        if (!string.IsNullOrEmpty(error)) return Done($"Spotify hat die Verbindung abgelehnt: {error}", false);
        if (string.IsNullOrEmpty(code)) return Done("Spotify hat keinen Code geliefert.", false);
        if (string.IsNullOrEmpty(state) || state != expected) return Done("Die Anfrage passt nicht zum gestarteten Login. Bitte nochmals verbinden.", false);

        try
        {
            var name = await spotifyComplete.HandleAsync(new CompleteSpotifyConnect.Command(code, RedirectUri()));
            return Done($"Spotify verbunden als {name}.", true);
        }
        catch (Exception ex)
        {
            return Done(ex.Message, false);
        }
    }

    [HttpGet("spotify/token")]
    public async Task<IActionResult> SpotifyToken()
    {
        var token = await spotifyToken.HandleAsync(new GetSpotifyAccessToken.Query());
        if (token is null) return StatusCode(409, new { error = "not-connected" });
        Response.Headers.CacheControl = "no-store";
        return Json(new { access_token = token, expires_in = 300 });
    }

    private string RedirectUri() => $"{Request.Scheme}://{Request.Host}/admin/show/spotify/callback";

    private ContentResult Done(string message, bool ok)
    {
        var colour = ok ? "#1ed760" : "#ff8a97";
        var hint = ok
            ? "Dieses Fenster kann geschlossen werden. Im Show-Admin den Spotify-Dialog neu öffnen."
            : "Dieses Fenster kann geschlossen werden.";
        var html = $"""
            <!doctype html>
            <html lang="de"><head><meta charset="utf-8"><title>Spotify</title></head>
            <body style="margin:0;display:flex;align-items:center;justify-content:center;height:100vh;background:#15181c;color:#e9edf1;font:16px/1.5 system-ui,sans-serif">
              <div style="max-width:34rem;padding:2rem;text-align:center">
                <h1 style="color:{colour};font-size:1.2rem;margin:0 0 .6rem">{System.Net.WebUtility.HtmlEncode(message)}</h1>
                <p style="color:#9aa4ad;margin:0">{hint}</p>
              </div>
            </body></html>
            """;
        return Content(html, "text/html");
    }
}
