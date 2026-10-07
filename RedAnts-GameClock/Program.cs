using RedAnts.GameClock;
using RedAnts.GameClock.Clocks;
using RedAnts.GameClock.Components;
using RedAnts.GameClock.Configuration;
using RedAnts.GameClock.Teams;
using RedAnts.GameClock.Vmix;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.Configure<GameClockOptions>(builder.Configuration.GetSection(GameClockOptions.Section));
builder.Services.AddSingleton<DataFolder>();
builder.Services.AddSingleton<ConfigStore>();
builder.Services.AddSingleton<ClockHub>();
builder.Services.AddSingleton<IClockProtocol, ICastProtocol>();
builder.Services.AddSingleton<IClockProtocol, DelimitedProtocol>();
builder.Services.AddSingleton<ClockProtocols>();
builder.Services.AddSingleton<ClockFeed>();
builder.Services.AddHostedService(services => services.GetRequiredService<ClockFeed>());
builder.Services.AddSingleton<ClockScanner>();
builder.Services.AddSingleton<TeamResolver>();
builder.Services.AddSingleton<TeamDirectory>();
builder.Services.AddSingleton<VmixPublisher>();
builder.Services.AddHostedService(services => services.GetRequiredService<VmixPublisher>());
builder.Services.AddHttpClient();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/api/state", (ClockHub hub) => hub.Current);

app.MapGet($"{LogoFiles.Route}/{{file}}", (string file, DataFolder data) =>
{
    if (file != Path.GetFileName(file)) return Results.BadRequest();
    var path = Path.Combine(data.Logos, file);
    return File.Exists(path) ? Results.File(path, LogoFiles.ContentType(file)) : Results.NotFound();
});

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Lifetime.ApplicationStarted.Register(() => StartupBanner.Print(app.Urls));
app.Run();
