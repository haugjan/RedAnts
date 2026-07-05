using System.Globalization;
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

var basicUser = app.Configuration["BasicAuth:Username"];
var basicPass = app.Configuration["BasicAuth:Password"];
if (!string.IsNullOrEmpty(basicUser) && !string.IsNullOrEmpty(basicPass))
{
    const string gateCookie = "RedAnts.Gate";
    var gateToken = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes($"{basicUser}:{basicPass}")));

    bool CredentialsMatch(string header)
    {
        const string scheme = "Basic ";
        if (!header.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            return false;
        byte[] raw;
        try { raw = Convert.FromBase64String(header[scheme.Length..].Trim()); }
        catch (FormatException) { return false; }
        foreach (var encoding in new[] { System.Text.Encoding.UTF8, System.Text.Encoding.Latin1 })
        {
            var pair = encoding.GetString(raw);
            var separator = pair.IndexOf(':');
            if (separator < 0) continue;
            if (pair[..separator] == basicUser && pair[(separator + 1)..] == basicPass)
                return true;
        }
        return false;
    }

    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/umbraco")
            || context.Request.Path.StartsWithSegments("/admin/ticketing")
            || context.Request.Path.StartsWithSegments("/_blazor")
            || context.Request.Path.StartsWithSegments("/_framework"))
        {
            await next();
            return;
        }
        if (context.Request.Cookies[gateCookie] == gateToken)
        {
            await next();
            return;
        }
        if (CredentialsMatch(context.Request.Headers.Authorization.ToString()))
        {
            context.Response.Cookies.Append(gateCookie, gateToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(7),
            });
            await next();
            return;
        }
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        var fetchMode = context.Request.Headers["Sec-Fetch-Mode"].ToString();
        var isNavigation = fetchMode.Length > 0
            ? string.Equals(fetchMode, "navigate", StringComparison.OrdinalIgnoreCase)
            : HttpMethods.IsGet(context.Request.Method)
              && context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
        if (isNavigation)
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"RedAnts\", charset=\"UTF-8\"";
    });
}

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/"
        && context.Request.Host.Host.StartsWith("scan.", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/scanntickets");
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
