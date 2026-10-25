using System.Security.Cryptography;
using System.Text;

namespace RedAnts.Show.Agent.Web;

public static class BoardGate
{
    const string Cookie = "RedAnts.AgentBoard";
    const string Path = "/bord/anmeldung";
    const string Page =
        "<!DOCTYPE html><html lang=\"de\"><head><meta charset=\"utf-8\">" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Bord</title><style>" +
        "body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;background:#0f0f13;color:#e9edf1;font-family:Verdana,sans-serif}" +
        "form{background:#16161d;padding:2rem;border-radius:12px;width:min(90vw,340px);text-align:center}" +
        "h1{font-size:1.1rem;margin:0 0 1.2rem}" +
        "input{width:100%;padding:.7rem;border:1px solid #2f2f3a;border-radius:6px;background:#0f0f13;color:#fff;font-size:1rem;box-sizing:border-box}" +
        "button{margin-top:1rem;width:100%;padding:.7rem;border:0;border-radius:6px;background:#d02d38;color:#fff;font-weight:700;font-size:1rem;cursor:pointer}" +
        "p{color:#ff9b9b;font-size:.9rem;margin:.8rem 0 0}" +
        "</style></head><body><form method=\"post\" action=\"{ACTION}\">" +
        "<h1>{HALL}</h1>" +
        "<input type=\"password\" name=\"password\" placeholder=\"Passwort\" autofocus autocomplete=\"current-password\">" +
        "<input type=\"hidden\" name=\"returnUrl\" value=\"{RETURN}\">" +
        "<button type=\"submit\">Weiter</button>{ERROR}</form></body></html>";

    static readonly HashSet<string> Tokens = [];

    public static void UseBoardGate(this WebApplication app, string password, string hall)
    {
        if (password.Length == 0) return;

        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            if (!path.StartsWithSegments("/bord") && !path.StartsWithSegments("/spiel"))
            {
                await next();
                return;
            }

            if (path.StartsWithSegments(Path))
            {
                if (!HttpMethods.IsPost(context.Request.Method))
                {
                    await WriteAsync(context, Returns(context.Request.Query["returnUrl"]), hall, false);
                    return;
                }
                var form = await context.Request.ReadFormAsync();
                var back = Returns(form["returnUrl"].ToString());
                if (!Matches(password, form["password"].ToString()))
                {
                    await WriteAsync(context, back, hall, true);
                    return;
                }
                Issue(context);
                context.Response.Redirect(back);
                return;
            }

            if (Allows(context))
            {
                await next();
                return;
            }

            await WriteAsync(context, context.Request.Path + context.Request.QueryString, hall, false);
        });
    }

    static string Returns(string? value) =>
        value is { Length: > 0 } && value.StartsWith('/') && !value.StartsWith("//") ? value : "/bord";

    static bool Matches(string password, string given) =>
        given.Length > 0 && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(given));

    static bool Allows(HttpContext context) =>
        context.Request.Cookies[Cookie] is { Length: > 0 } token && Tokens.Contains(token);

    static void Issue(HttpContext context)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        lock (Tokens) Tokens.Add(token);
        context.Response.Cookies.Append(Cookie, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
        });
    }

    static async Task WriteAsync(HttpContext context, string back, string hall, bool failed)
    {
        context.Response.StatusCode = failed ? StatusCodes.Status401Unauthorized : StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        var action = System.Net.WebUtility.HtmlEncode($"{Path}?returnUrl={Uri.EscapeDataString(back)}");
        await context.Response.WriteAsync(Page
            .Replace("{ACTION}", action)
            .Replace("{HALL}", System.Net.WebUtility.HtmlEncode(hall))
            .Replace("{RETURN}", System.Net.WebUtility.HtmlEncode(back))
            .Replace("{ERROR}", failed ? "<p>Falsches Passwort.</p>" : ""));
    }
}
