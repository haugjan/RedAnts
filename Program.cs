using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.StaticFiles;
using RedAnts.Features.Ticketing.Cart;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing;
using Umbraco.StorageProviders.AzureBlob.IO;

var swissCulture = new CultureInfo("de-CH");
CultureInfo.DefaultThreadCurrentCulture = swissCulture;
CultureInfo.DefaultThreadCurrentUICulture = swissCulture;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

var sessionCacheConnectionString = builder.Configuration.GetConnectionString("umbracoDbDSN");
if (!string.IsNullOrWhiteSpace(sessionCacheConnectionString))
{
    SessionCacheSchema.Ensure(sessionCacheConnectionString);
    builder.Services.AddDistributedSqlServerCache(options =>
    {
        options.ConnectionString = sessionCacheConnectionString;
        options.SchemaName = SessionCacheSchema.SchemaName;
        options.TableName = SessionCacheSchema.TableName;
    });
}

builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RedAnts.Cart";
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(7);
});
builder.Services.AddScoped<ICartService, SessionCartService>();

var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers();

if (!string.IsNullOrWhiteSpace(builder.Configuration["Umbraco:Storage:AzureBlob:Media:ConnectionString"]))
{
    umbracoBuilder.AddAzureBlobMediaFileSystem();
}

if (builder.Environment.IsDevelopment())
{
    builder.Services.PostConfigure<OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreOptions>(options =>
        options.DisableTransportSecurityRequirement = true);
}

umbracoBuilder.Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

const string publicCsp =
    "default-src 'self'; base-uri 'self'; object-src 'none'; " +
    "img-src 'self' data: https:; " +
    "font-src 'self' https://fonts.gstatic.com data:; " +
    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
    "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://challenges.cloudflare.com https://media.payrexx.com; " +
    "connect-src 'self' https://challenges.cloudflare.com https://*.payrexx.com; " +
    "frame-src 'self' https://www.google.com https://challenges.cloudflare.com https://payrexx.com https://*.payrexx.com; " +
    "form-action 'self' https://payrexx.com https://*.payrexx.com; " +
    "frame-ancestors 'self'";

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        var path = context.Request.Path;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers.Remove("X-Powered-By");

        if (context.Request.IsHttps)
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        var isEmbed = path.Equals("/next/embed", StringComparison.OrdinalIgnoreCase);
        if (!isEmbed)
            headers["X-Frame-Options"] = "SAMEORIGIN";

        var cspExempt = isEmbed
            || path.StartsWithSegments("/umbraco")
            || path.StartsWithSegments("/App_Plugins")
            || path.StartsWithSegments("/_blazor")
            || path.StartsWithSegments("/_framework")
            || path.StartsWithSegments("/admin")
            || path.StartsWithSegments("/scan")
            || path.StartsWithSegments("/scanner-test");
        if (!cspExempt && !headers.ContainsKey("Content-Security-Policy"))
            headers["Content-Security-Policy"] = publicCsp;

        return Task.CompletedTask;
    });
    await next();
});

var notFoundPage = Path.Combine(app.Environment.WebRootPath, "404.html");
var notFoundHtml = File.Exists(notFoundPage) ? await File.ReadAllTextAsync(notFoundPage) : "<h1>404</h1>";

app.Use(async (context, next) =>
{
    var request = context.Request;
    var handle404 = HttpMethods.IsGet(request.Method)
        && request.Path.HasValue
        && !Path.HasExtension(request.Path.Value)
        && !request.Path.StartsWithSegments("/umbraco")
        && !request.Path.StartsWithSegments("/App_Plugins")
        && !request.Path.StartsWithSegments("/_blazor")
        && !request.Path.StartsWithSegments("/_framework")
        && !request.Path.StartsWithSegments("/api")
        && !request.Path.StartsWithSegments("/ticket")
        && !request.Path.StartsWithSegments("/warmup");

    if (!handle404)
    {
        await next();
        return;
    }

    var originalBody = context.Response.Body;
    await using var buffer = new MemoryStream();
    context.Response.Body = buffer;
    try
    {
        await next();
        context.Response.Body = originalBody;
        if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
        {
            context.Response.Headers.ContentLength = null;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(notFoundHtml);
        }
        else
        {
            buffer.Seek(0, SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody);
        }
    }
    finally
    {
        context.Response.Body = originalBody;
    }
});

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        var host = context.Request.Host.Host;
        var isScanHost = host.StartsWith("scan.", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("scan-dev.", StringComparison.OrdinalIgnoreCase);
        var isAdminHost = host.StartsWith("admin.", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("admin-dev.", StringComparison.OrdinalIgnoreCase);
        context.Response.Redirect(isScanHost ? "/scan" : isAdminHost ? "/umbraco" : "/ticketing/");
        return;
    }
    await next();
});

var gatePassword = app.Configuration["BasicAuth:Password"];
if (!string.IsNullOrEmpty(gatePassword))
{
    const string gateCookie = "RedAnts.Gate";
    const string gatePath = "/__gate";
    const string gatePageHtml =
        "<!DOCTYPE html><html lang=\"de\"><head><meta charset=\"utf-8\">" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Red Ants</title>" +
        "<style>" +
        "body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;background:#1b1b1b;color:#fff;font-family:Verdana,sans-serif}" +
        "form{background:#262626;padding:2rem;border-radius:12px;width:min(90vw,340px);text-align:center}" +
        "h1{font-size:1.15rem;margin:0 0 1.2rem}" +
        "input{width:100%;padding:.7rem;border:1px solid #444;border-radius:6px;background:#1b1b1b;color:#fff;font-size:1rem;box-sizing:border-box}" +
        "button{margin-top:1rem;width:100%;padding:.7rem;border:0;border-radius:6px;background:#C8102E;color:#fff;font-weight:700;font-size:1rem;cursor:pointer}" +
        ".err{color:#ff8080;font-size:.9rem;margin:.7rem 0 0}" +
        "</style></head><body>" +
        "<form method=\"post\" action=\"{ACTION}\">" +
        "<h1>Red Ants – Testzugang</h1>" +
        "<input type=\"password\" name=\"password\" placeholder=\"Passwort\" autofocus autocomplete=\"current-password\">" +
        "<input type=\"hidden\" name=\"returnUrl\" value=\"{RETURN}\">" +
        "<button type=\"submit\">Weiter</button>{ERROR}" +
        "</form></body></html>";

    var cookieDomain = app.Configuration["BasicAuth:CookieDomain"];
    var protector = app.Services
        .GetRequiredService<IDataProtectionProvider>()
        .CreateProtector("RedAnts.SiteGate.v1");

    static bool IsExempt(PathString path) =>
        path.StartsWithSegments("/umbraco")
        || path.StartsWithSegments("/admin/ticketing")
        || path.StartsWithSegments("/App_Plugins")
        || path.StartsWithSegments("/_blazor")
        || path.StartsWithSegments("/_framework")
        || path.StartsWithSegments("/ticket")
        || path.StartsWithSegments("/css")
        || path.StartsWithSegments("/js")
        || path.StartsWithSegments("/img")
        || path.StartsWithSegments("/media")
        || path.StartsWithSegments("/favicons")
        || path.StartsWithSegments("/lib")
        || path.StartsWithSegments("/impressum")
        || path.StartsWithSegments("/datenschutz")
        || path.StartsWithSegments("/agb")
        || path.StartsWithSegments("/scanner-test")
        || path.StartsWithSegments("/scan")
        || path.StartsWithSegments("/scan")
        || path.StartsWithSegments("/payrexx/webhook")
        || path.StartsWithSegments("/warmup")
        || path == "/favicon.ico"
        || path == "/scanner-sw.js"
        || path == "/site.webmanifest";

    static string SafeReturn(string? value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith('/') && !value.StartsWith("//") ? value : "/";

    bool HasGate(HttpContext ctx)
    {
        var value = ctx.Request.Cookies[gateCookie];
        if (string.IsNullOrEmpty(value)) return false;
        try { return protector.Unprotect(value) == "ok"; }
        catch { return false; }
    }

    void SetGateCookie(HttpContext ctx) =>
        ctx.Response.Cookies.Append(gateCookie, protector.Protect("ok"), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
            Domain = string.IsNullOrWhiteSpace(cookieDomain) ? null : cookieDomain,
        });

    async Task WriteGate(HttpContext ctx, string returnUrl, bool failed)
    {
        ctx.Response.StatusCode = failed ? StatusCodes.Status401Unauthorized : StatusCodes.Status200OK;
        ctx.Response.ContentType = "text/html; charset=utf-8";
        var action = System.Net.WebUtility.HtmlEncode($"{gatePath}?returnUrl={Uri.EscapeDataString(returnUrl)}");
        await ctx.Response.WriteAsync(gatePageHtml
            .Replace("{ACTION}", action)
            .Replace("{RETURN}", System.Net.WebUtility.HtmlEncode(returnUrl))
            .Replace("{ERROR}", failed ? "<p class=\"err\">Falsches Passwort.</p>" : ""));
    }

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

    async Task<(string Name, bool AllEvents, string EventIds)?> HelperSessionAsync(HttpContext ctx)
    {
        var value = ctx.Request.Cookies[helperCookie];
        if (string.IsNullOrEmpty(value)) return null;
        int id;
        try { id = int.Parse(helperProtector.Unprotect(value)); } catch { return null; }
        var helper = await ctx.RequestServices.GetRequiredService<IHelpers>().FindByIdAsync(id);
        return helper is { Active: true }
            ? (helper.FullName, helper.AllEvents, string.Join(",", helper.EventIds))
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
        }

        await next();
    });

    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments(gatePath))
        {
            if (HttpMethods.IsPost(context.Request.Method))
            {
                var form = await context.Request.ReadFormAsync();
                var returnUrl = SafeReturn(form["returnUrl"].ToString());
                if (form["password"].ToString() == gatePassword)
                {
                    SetGateCookie(context);
                    context.Response.Redirect(returnUrl);
                    return;
                }
                await WriteGate(context, returnUrl, true);
                return;
            }
            await WriteGate(context, SafeReturn(context.Request.Query["returnUrl"].ToString()), false);
            return;
        }

        if (context.Request.Query["key"].ToString() == gatePassword)
        {
            SetGateCookie(context);
            await next();
            return;
        }

        if (IsExempt(context.Request.Path) || HasGate(context))
        {
            await next();
            return;
        }

        var target = context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"{gatePath}?returnUrl={Uri.EscapeDataString(target)}");
    });
}


var staticFileContentTypes = new FileExtensionContentTypeProvider();
staticFileContentTypes.Mappings[".webmanifest"] = "application/manifest+json";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = staticFileContentTypes });

app.UseSession();

app.MapBlazorHub();
app.MapRazorPages();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
