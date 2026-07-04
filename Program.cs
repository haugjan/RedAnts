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
    // After a browser passes the Basic challenge once we drop an unlock cookie and accept it on
    // subsequent requests. Cookies — unlike cached HTTP Basic credentials — ride along on the Blazor
    // Server SignalR WebSocket handshake (/_blazor) and on every background fetch, so those requests
    // no longer hit the challenge. Soft gate, not real security: the cookie is just a non-reversible
    // marker (SHA-256 of the credentials) proving the password was entered.
    const string gateCookie = "RedAnts.Gate";
    var gateToken = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes($"{basicUser}:{basicPass}")));

    // Decode the incoming "Authorization: Basic …" header and compare the credentials. Browsers
    // encode "user:pass" as UTF-8 (when the charset hint is honoured) or as Latin-1 — accept both,
    // otherwise a password with umlauts is never accepted and the dialog reappears forever.
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
        // The Umbraco backoffice has its own login and authenticates its API calls with a Bearer token,
        // which would never match the expected "Basic …" string → the gate would 401 every backoffice
        // request and the login loops. Let /umbraco through; it is protected by the Umbraco login.
        if (context.Request.Path.StartsWithSegments("/umbraco"))
        {
            await next();
            return;
        }
        // Already unlocked in this browser (cookie set after the first successful Basic challenge).
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
        // Only a 401 that carries "WWW-Authenticate" pops the browser's native password dialog. Send it
        // exclusively on top-level page navigations, so background requests without the cookie (a Blazor
        // reconnect, a prefetch, a favicon) fail silently instead of re-prompting for the password on
        // every page. The next navigation still challenges and, once entered, sets the cookie for all.
        var fetchMode = context.Request.Headers["Sec-Fetch-Mode"].ToString();
        var isNavigation = fetchMode.Length > 0
            ? string.Equals(fetchMode, "navigate", StringComparison.OrdinalIgnoreCase)
            : HttpMethods.IsGet(context.Request.Method)
              && context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
        if (isNavigation)
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"RedAnts\", charset=\"UTF-8\"";
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
