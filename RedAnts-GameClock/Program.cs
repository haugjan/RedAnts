using RedAnts.GameClock;
using RedAnts.GameClock.Components;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.Configure<GameClockOptions>(builder.Configuration.GetSection(GameClockOptions.Section));
builder.Services.AddSingleton<ClockHub>();
builder.Services.AddHostedService<UdpReceiver>();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/api/state", (ClockHub hub) => hub.Current);
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Lifetime.ApplicationStarted.Register(() => StartupBanner.Print(app.Urls));
app.Run();
