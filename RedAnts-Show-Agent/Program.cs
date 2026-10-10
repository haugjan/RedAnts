using RedAnts.Show.Agent;
using RedAnts.Show.Agent.Clock;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Deck;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Scenes;
using RedAnts.Show.Agent.Vmix;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var stored = AgentConfig.Load();
var config = stored.WithArgs(args).Prompted();
if (config != stored) config.Save();
if (!config.IsComplete)
{
    Console.Error.WriteLine("Keine vollständige Einrichtung: Cloud-URL, Hallen-Kennung und Agent-Schlüssel fehlen.");
    Console.Error.WriteLine($"Entweder {AgentConfig.File} ausfüllen oder starten mit --cloud <url> --hall <kennung> --key <schlüssel>.");
    return 1;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://localhost:{config.DeckPort}");
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<PayloadStore>();
builder.Services.AddSingleton<LiveGame>();
builder.Services.AddSingleton<SednaLink>();
builder.Services.AddSingleton<VmixLink>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<VmixLink>());
builder.Services.AddSingleton<ClockFeed>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ClockFeed>());
builder.Services.AddSingleton<SceneRunner>();
builder.Services.AddSingleton<Deck>();
builder.Services.AddSingleton<CloudLink>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CloudLink>());
builder.Services.AddSingleton<StatusReporter>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<StatusReporter>());

var app = builder.Build();
var deck = app.Services.GetRequiredService<Deck>();
var reporter = app.Services.GetRequiredService<StatusReporter>();
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

app.MapGet("/", () => Results.Text(
    $"RedAnts Show-Agent {AgentConfig.Version}\nHalle: {config.HallId}\nCloud: {config.CloudUrl}\nDeck: http://localhost:{config.DeckPort}/deck/view\nStatus: /status\n"));

app.MapGet("/status", () => Results.Json(reporter.Status(), PayloadJson.Options));

app.MapGet("/deck/view", async (long? since) =>
{
    await deck.WaitAsync(since ?? 0, TimeSpan.FromSeconds(25), lifetime.ApplicationStopping);
    return Results.Json(deck.View());
});

app.MapGet("/deck/image", (string? key) => Results.Json(new { image = DeckIcons.FromKey(key ?? "") }));

app.MapPost("/deck/press/{number:int}", async (int number) =>
{
    var note = await deck.PressAsync(number);
    return Results.Json(new { ok = note is null, note });
});

Console.WriteLine($"RedAnts Show-Agent {AgentConfig.Version}: Halle {config.HallId}, Cloud {config.CloudUrl}, Deck auf http://localhost:{config.DeckPort}");
app.Run();
return 0;
