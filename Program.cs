using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.StaticFiles;
using RedAnts.Features.Ticketing.Cart;
using RedAnts.Infrastructure.Ticketing;
using Umbraco.StorageProviders.AzureBlob.IO;

var swissCulture = new CultureInfo("de-CH");
CultureInfo.DefaultThreadCurrentCulture = swissCulture;
CultureInfo.DefaultThreadCurrentUICulture = swissCulture;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

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
        || path.StartsWithSegments("/_blazor")
        || path.StartsWithSegments("/_framework");

    static string SafeReturn(string? value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith('/') && !value.StartsWith("//") ? value : "/";

    bool HasGate(HttpContext ctx)
    {
        var value = ctx.Request.Cookies[gateCookie];
        if (string.IsNullOrEmpty(value)) return false;
        try { return protector.Unprotect(value) == "ok"; }
        catch { return false; }
    }

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
                    context.Response.Cookies.Append(gateCookie, protector.Protect("ok"), new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        MaxAge = TimeSpan.FromDays(30),
                        Domain = string.IsNullOrWhiteSpace(cookieDomain) ? null : cookieDomain,
                    });
                    context.Response.Redirect(returnUrl);
                    return;
                }
                await WriteGate(context, returnUrl, true);
                return;
            }
            await WriteGate(context, SafeReturn(context.Request.Query["returnUrl"].ToString()), false);
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

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        var isScanHost = context.Request.Host.Host.StartsWith("scan.", StringComparison.OrdinalIgnoreCase);
        context.Response.Redirect(isScanHost ? "/scanntickets" : "/ticketing/");
        return;
    }
    await next();
});

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
