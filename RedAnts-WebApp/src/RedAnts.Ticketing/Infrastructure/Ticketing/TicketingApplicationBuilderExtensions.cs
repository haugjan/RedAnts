using Microsoft.AspNetCore.DataProtection;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing.Analytics;

namespace RedAnts.Infrastructure.Ticketing;

public static class TicketingApplicationBuilderExtensions
{
    public static WebApplication UseTicketingShortHostRedirect(this WebApplication app)
    {
        var shortBaseUrl = app.Configuration["Tickets:ShortBaseUrl"];
        var shortHost = Uri.TryCreate(shortBaseUrl, UriKind.Absolute, out var shortUri) ? shortUri.Host : null;
        var ticketMainBase = (app.Configuration["Tickets:PublicBaseUrl"] ?? "").TrimEnd('/');

        app.Use(async (context, next) =>
        {
            if (shortHost is not null && context.Request.Host.Host.Equals(shortHost, StringComparison.OrdinalIgnoreCase))
            {
                var path = context.Request.Path.Value ?? "/";
                var location = path == "/"
                    ? $"{ticketMainBase}/ticketing/"
                    : $"{ticketMainBase}/ticket{path}{context.Request.QueryString}";
                context.Response.Redirect(location);
                return;
            }
            await next();
        });

        return app;
    }

    public static WebApplication UseTicketingAnalytics(this WebApplication app)
    {
        var pageViewTracker = app.Services.GetRequiredService<IPageViewTracker>();
        var pageViewSalt = app.Configuration["Analytics:Salt"] ?? "redants-analytics-v1";

        app.Use(async (context, next) =>
        {
            await next();

            var request = context.Request;
            if (!HttpMethods.IsGet(request.Method)) return;
            if (context.Response.StatusCode != StatusCodes.Status200OK) return;
            if (!(context.Response.ContentType ?? "").Contains("text/html", StringComparison.OrdinalIgnoreCase)) return;

            var host = request.Host.Host;
            if (host.Contains("-dev.", StringComparison.OrdinalIgnoreCase)
                || host.StartsWith("scan", StringComparison.OrdinalIgnoreCase)
                || host.StartsWith("admin", StringComparison.OrdinalIgnoreCase)
                || host.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith("azurewebsites.net", StringComparison.OrdinalIgnoreCase)) return;

            var path = request.Path;
            if (!path.HasValue
                || Path.HasExtension(path.Value)
                || path.StartsWithSegments("/umbraco")
                || path.StartsWithSegments("/App_Plugins")
                || path.StartsWithSegments("/_blazor")
                || path.StartsWithSegments("/_content")
                || path.StartsWithSegments("/_framework")
                || path.StartsWithSegments("/api")
                || path.StartsWithSegments("/scan")
                || path.StartsWithSegments("/admin")
                || path.StartsWithSegments("/ticket")
                || path.StartsWithSegments("/warmup")
                || path.StartsWithSegments("/__gate")) return;

            var ua = request.Headers.UserAgent.ToString();
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "";
            var now = DateTime.UtcNow;
            var seed = $"{ip}|{ua}|{now:yyyyMMdd}|{pageViewSalt}";
            var visitorHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(seed)));

            var value = path.Value!;
            if (value.Length > 400) value = value[..400];

            pageViewTracker.Track(new PageView(now, value, visitorHash, LooksLikeBot(ua)));
        });

        return app;
    }

    public static WebApplication UseTicketingScanAuth(this WebApplication app)
    {
        var cookieDomain = app.Configuration["BasicAuth:CookieDomain"];

        const string helperCookie = "RedAnts.Helper";
        var helperProtector = app.Services
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("RedAnts.HelperSession.v1");

        const string helperPageHtml =
            "<!DOCTYPE html><html lang=\"de\"><head><meta charset=\"utf-8\">" +
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Scanner-Login</title>" +
            "<style>" +
            "body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;background:#1b1b1b;color:#fff;font-family:Verdana,sans-serif}" +
            "form{background:#262626;padding:2rem;border-radius:12px;width:min(90vw,340px);text-align:center}" +
            "h1{font-size:1.15rem;margin:0 0 .4rem}p.sub{color:#aaa;font-size:.85rem;margin:0 0 1.2rem}" +
            "input{width:100%;padding:.7rem;border:1px solid #444;border-radius:6px;background:#1b1b1b;color:#fff;font-size:1rem;box-sizing:border-box}" +
            "button{margin-top:1rem;width:100%;padding:.7rem;border:0;border-radius:6px;background:#C8102E;color:#fff;font-weight:700;font-size:1rem;cursor:pointer}" +
            ".err{color:#ff8080;font-size:.9rem;margin:.7rem 0 0}" +
            "</style></head><body>" +
            "<form method=\"post\" action=\"/scan/login\">" +
            "<h1>Red Ants – Scanner</h1><p class=\"sub\">Bitte dein Helfer-Passwort eingeben.</p>" +
            "<input type=\"text\" name=\"password\" placeholder=\"Passwort\" autofocus autocapitalize=\"none\" autocomplete=\"off\">" +
            "<button type=\"submit\">Anmelden</button>{ERROR}" +
            "</form></body></html>";

        void SetHelperCookie(HttpContext ctx, int helperId) =>
            ctx.Response.Cookies.Append(helperCookie, helperProtector.Protect(helperId.ToString()), new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(120),
                Domain = string.IsNullOrWhiteSpace(cookieDomain) ? null : cookieDomain,
            });

        async Task<(string Name, bool AllEvents, string EventIds, bool CanRebook)?> HelperSessionAsync(HttpContext ctx)
        {
            var value = ctx.Request.Cookies[helperCookie];
            if (string.IsNullOrEmpty(value)) return null;
            int id;
            try { id = int.Parse(helperProtector.Unprotect(value)); } catch { return null; }
            var helper = await ctx.RequestServices.GetRequiredService<IHelpers>().FindByIdAsync(id);
            return helper is { Active: true }
                ? (helper.FullName, helper.AllEvents, string.Join(",", helper.EventIds), helper.CanRebook)
                : null;
        }

        async Task WriteHelperLogin(HttpContext ctx, bool failed)
        {
            ctx.Response.StatusCode = failed ? StatusCodes.Status401Unauthorized : StatusCodes.Status200OK;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(helperPageHtml
                .Replace("{ERROR}", failed ? "<p class=\"err\">Passwort nicht erkannt.</p>" : ""));
        }

        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;

            if (path == "/scan/login")
            {
                if (HttpMethods.IsPost(context.Request.Method))
                {
                    var form = await context.Request.ReadFormAsync();
                    var helper = await context.RequestServices.GetRequiredService<IHelpers>()
                        .FindByPasswordAsync(form["password"].ToString());
                    if (helper is not null)
                    {
                        SetHelperCookie(context, helper.Id);
                        context.Response.Redirect("/scan");
                        return;
                    }
                    await WriteHelperLogin(context, true);
                    return;
                }
                await WriteHelperLogin(context, false);
                return;
            }

            if (path == "/scan/logout")
            {
                context.Response.Cookies.Delete(helperCookie, new CookieOptions
                {
                    Domain = string.IsNullOrWhiteSpace(cookieDomain) ? null : cookieDomain,
                });
                context.Response.Redirect("/scan/login");
                return;
            }

            if (path.StartsWithSegments("/scan", out var rest) && rest.HasValue && rest.Value.Trim('/').Length > 0)
            {
                var code = Uri.UnescapeDataString(rest.Value.Trim('/'));
                var helper = await context.RequestServices.GetRequiredService<IHelpers>().FindByPasswordAsync(code);
                if (helper is not null) SetHelperCookie(context, helper.Id);
                context.Response.Redirect("/scan");
                return;
            }

            if (path.StartsWithSegments("/scan"))
            {
                var session = await HelperSessionAsync(context);
                if (session is not { } helper)
                {
                    context.Response.Redirect("/scan/login");
                    return;
                }
                context.Items["HelperName"] = helper.Name;
                context.Items["HelperAllEvents"] = helper.AllEvents;
                context.Items["HelperEventIds"] = helper.EventIds;
                context.Items["HelperCanRebook"] = helper.CanRebook;
            }

            await next();
        });

        return app;
    }

    private static bool LooksLikeBot(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return true;
        var ua = userAgent.ToLowerInvariant();
        string[] tokens =
        [
            "bot", "crawl", "spider", "slurp", "bingpreview", "facebookexternalhit", "embedly", "quora",
            "pinterest", "semrush", "ahrefs", "mj12", "dotbot", "petalbot", "bytespider", "google-read-aloud",
            "headlesschrome", "monitor", "uptime", "curl", "wget", "python-requests", "go-http", "scrapy",
            "whatsapp", "telegrambot", "discordbot", "linkedinbot", "applebot", "yandex", "duckduckbot"
        ];
        return tokens.Any(t => ua.Contains(t));
    }
}
