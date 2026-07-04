using System.Globalization;
using Microsoft.AspNetCore.StaticFiles;
using RedAnts.Features.Ticketing.Cart;
using RedAnts.Infrastructure.Ticketing;
using Umbraco.StorageProviders.AzureBlob.IO;

// Swiss German as the default culture for all threads.
// This makes Blazor @bind, implicit ToString() calls, and number/date parsing
// use Swiss format (dd.MM.yyyy dates, HH:mm times, apostrophe thousands separator).
var swissCulture = new CultureInfo("de-CH");
CultureInfo.DefaultThreadCurrentCulture = swissCulture;
CultureInfo.DefaultThreadCurrentUICulture = swissCulture;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Persistence is Azure SQL in every environment (dev + prod), so there is no SQLite bootstrap.
// Local development connects to the shared DEV Azure SQL via user secrets (see appsettings.Development.json).

// Prevent app crash when a background service hits a transient SQL timeout.
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

// Guest shopping cart stored in the session.
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

// Store Umbraco media in Azure Blob Storage when configured (production). The connection string
// and container come from configuration (Umbraco:Storage:AzureBlob:Media:*), injected as App
// Service app settings. Registered only when a connection string is present so local dev (no blob
// config, media on disk) is unaffected and the provider's options validation does not fail on boot.
if (!string.IsNullOrWhiteSpace(builder.Configuration["Umbraco:Storage:AzureBlob:Media:ConnectionString"]))
{
    umbracoBuilder.AddAzureBlobMediaFileSystem();
}

// Allow HTTP in local development (OpenIddict requires HTTPS by default).
if (builder.Environment.IsDevelopment())
{
    builder.Services.PostConfigure<OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreOptions>(options =>
        options.DisableTransportSecurityRequirement = true);
}

umbracoBuilder.Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

// --- Live-test lockdown: HTTP Basic gate over everything ------------------------------------------
// When BasicAuth:Username/Password are configured (live App Service settings), every request must
// carry matching HTTP Basic credentials — backoffice, ticket links and webhooks included ("alles
// dicht" for a closed test). Left unset locally, so development is unaffected. Soft gate, not real
// security. Runs before static files so nothing is served unauthenticated.
var basicUser = app.Configuration["BasicAuth:Username"];
var basicPass = app.Configuration["BasicAuth:Password"];
if (!string.IsNullOrEmpty(basicUser) && !string.IsNullOrEmpty(basicPass))
{
    var expectedAuth = "Basic " + Convert.ToBase64String(
        System.Text.Encoding.UTF8.GetBytes($"{basicUser}:{basicPass}"));
    app.Use(async (context, next) =>
    {
        // The Umbraco backoffice has its own login and authenticates its API calls with a Bearer token,
        // which would never match the expected "Basic …" string → the gate would 401 every backoffice
        // request and the login loops. Let /umbraco through; it is protected by the Umbraco login.
        if (context.Request.Path.StartsWithSegments("/umbraco"))
        {
            await next();
            return;
        }
        if (context.Request.Headers.Authorization.ToString() == expectedAuth)
        {
            await next();
            return;
        }
        context.Response.Headers.WWWAuthenticate = "Basic realm=\"RedAnts\", charset=\"UTF-8\"";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    });
}

// --- Host-based landing for the scan subdomain ---------------------------------------------------
// scan.redants.ch → scanner (/scanntickets). tickets.redants.ch serves the normal site: its root is the
// homepage with the event list, which is the public sales entry, so it is left untouched.
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

// Ensure the PWA manifest is served with the correct MIME type (.webmanifest).
var staticFileContentTypes = new FileExtensionContentTypeProvider();
staticFileContentTypes.Mappings[".webmanifest"] = "application/manifest+json";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = staticFileContentTypes });

// Session must be available to the cart before the Umbraco/MVC endpoints run.
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
