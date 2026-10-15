using RedAnts.Show.Simulator.Components;
using RedAnts.Show.Simulator.Simulation;

var builder = WebApplication.CreateBuilder(args);
if (!args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase)))
    builder.WebHost.UseUrls("http://localhost:5090");
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<EventLog>();
builder.Services.AddSingleton<ClockSender>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ClockSender>());
builder.Services.AddSingleton<TitleLibrary>();
builder.Services.AddSingleton<VmixStub>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<VmixStub>());
builder.Services.AddSingleton<SednaStub>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SednaStub>());
builder.Services.AddSingleton<AgentProbe>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AgentProbe>());

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

var urls = app.Configuration["urls"] ?? "http://localhost:5090";
Console.WriteLine($"RedAnts Show-Simulator: {urls}  (Matchuhr sendet an UDP 50085, vMix-Stellvertreter auf TCP 8099, sedna-Stellvertreter auf TCP 27182)");
app.Run();
