using System.Globalization;
using Microsoft.AspNetCore.StaticFiles;

// Swiss German as the default culture for all threads.
// This makes Blazor @bind, implicit ToString() calls, and number/date parsing
// use Swiss format (dd.MM.yyyy dates, HH:mm times, apostrophe thousands separator).
var swissCulture = new CultureInfo("de-CH");
CultureInfo.DefaultThreadCurrentCulture = swissCulture;
CultureInfo.DefaultThreadCurrentUICulture = swissCulture;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Resolve SQLite Data Source to an absolute path using the content root, and pre-enable
// WAL mode before Umbraco boots. WAL prevents SQLITE_BUSY when Umbraco's migrator,
// OpenIddict/EF Core and NPoco repositories open the same file concurrently at startup.
var sqliteProviderKey = "ConnectionStrings:umbracoDbDSN_ProviderName";
if (builder.Configuration[sqliteProviderKey] == "Microsoft.Data.Sqlite")
{
    const string connKey = "ConnectionStrings:umbracoDbDSN";
    var connStr = builder.Configuration[connKey] ?? string.Empty;
    const string prefix = "Data Source=";
    var start = connStr.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
    if (start >= 0)
    {
        var valueStart = start + prefix.Length;
        var end = connStr.IndexOf(';', valueStart);
        var dataSource = end >= 0 ? connStr[valueStart..end] : connStr[valueStart..];
        var absDataSource = Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.GetFullPath(dataSource.Replace('/', Path.DirectorySeparatorChar),
                               builder.Environment.ContentRootPath);
        var resolved = $"Data Source={absDataSource};Foreign Keys=True;Pooling=True";
        builder.Configuration[connKey] = resolved;

        Directory.CreateDirectory(Path.GetDirectoryName(absDataSource)!);
        using var init = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={absDataSource}");
        init.Open();
        using var wal = init.CreateCommand();
        wal.CommandText = "PRAGMA journal_mode=WAL;";
        wal.ExecuteNonQuery();
    }
}

// Prevent app crash when a background service hits a transient SQL timeout.
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers();

// Allow HTTP in local development (OpenIddict requires HTTPS by default).
if (builder.Environment.IsDevelopment())
{
    builder.Services.PostConfigure<OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreOptions>(options =>
        options.DisableTransportSecurityRequirement = true);
}

umbracoBuilder.Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

// Ensure the PWA manifest is served with the correct MIME type (.webmanifest).
var staticFileContentTypes = new FileExtensionContentTypeProvider();
staticFileContentTypes.Mappings[".webmanifest"] = "application/manifest+json";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = staticFileContentTypes });

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
