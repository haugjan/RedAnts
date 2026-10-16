using RedAnts.Show.Agent;
using RedAnts.Show.Agent.Clock;
using RedAnts.Show.Agent.Components;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Deck;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Music;
using RedAnts.Show.Agent.Keys;
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
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton(config);
builder.Services.AddSingleton<PayloadStore>();
builder.Services.AddSingleton<LiveGame>();
builder.Services.AddSingleton<ShowStates>();
builder.Services.AddSingleton<SednaLink>();
builder.Services.AddSingleton<VmixLink>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<VmixLink>());
builder.Services.AddSingleton<ClockFeed>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ClockFeed>());
builder.Services.AddSingleton<KeyRunner>();
builder.Services.AddSingleton<Deck>();
builder.Services.AddSingleton<CloudLink>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CloudLink>());
builder.Services.AddSingleton<MusicCache>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MusicCache>());
builder.Services.AddHostedService<BreakMusic>();
builder.Services.AddSingleton<DeckPublisher>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DeckPublisher>());
builder.Services.AddSingleton<StatusReporter>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<StatusReporter>());

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
var deck = app.Services.GetRequiredService<Deck>();
var reporter = app.Services.GetRequiredService<StatusReporter>();
var runner = app.Services.GetRequiredService<KeyRunner>();
var states = app.Services.GetRequiredService<ShowStates>();
var vmixLink = app.Services.GetRequiredService<VmixLink>();
var live = app.Services.GetRequiredService<LiveGame>();
live.Changed += () => states.Set(ShowStates.ClockRunning, live.ClockRunning ? "läuft" : "steht", "Matchuhr");
runner.MusicRequested += music => reporter.ReportMusic(music);
vmixLink.InputsRead += inputs => reporter.ReportInputs(inputs);
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

app.MapGet("/info", () => Results.Text(
    $"RedAnts Show-Agent {AgentConfig.Version}\nHalle: {config.HallId}\nCloud: {config.CloudUrl}\nBord: http://localhost:{config.DeckPort}/bord\nMatchuhr: http://localhost:{config.DeckPort}/uhr\nStatus: /status\n"));

app.MapGet("/status", () => Results.Json(reporter.Status(), PayloadJson.Options));

var music = app.Services.GetRequiredService<MusicCache>();

app.MapGet("/dj/sound/{**reference}", async (string reference, CancellationToken stop) =>
{
    var file = await music.EnsureAsync(reference, stop);
    return file is null ? Results.NotFound() : Results.File(file, MusicTypes.Of(file), enableRangeProcessing: true);
});

app.MapGet("/deck/view", async (string? deckId, long? since) =>
{
    var id = deckId is { Length: > 0 } ? deckId : deck.Decks.FirstOrDefault() ?? "";
    await deck.WaitAsync(since ?? 0, TimeSpan.FromSeconds(25), lifetime.ApplicationStopping);
    return Results.Json(deck.View(id), PayloadJson.Options);
});

app.MapPost("/deck/music/{what}", async (string what) =>
{
    await deck.MusicControlAsync(what);
    return Results.Json(new { ok = true });
});

app.MapPost("/deck/home", (string? deckId) =>
{
    var id = deckId is { Length: > 0 } ? deckId : deck.Decks.FirstOrDefault() ?? "";
    deck.GoHome(id);
    return Results.Json(new { ok = true });
});

app.MapGet("/deck/image", (string? key) => Results.Json(new { image = DeckIcons.FromKey(key ?? "") }));

app.MapPost("/deck/press/{number:int}", async (int number, string? deckId) =>
{
    var id = deckId is { Length: > 0 } ? deckId : deck.Decks.FirstOrDefault() ?? "";
    var note = await deck.PressAsync(id, number);
    return Results.Json(new { ok = note is null, note });
});

Console.WriteLine($"RedAnts Show-Agent {AgentConfig.Version}: Halle {config.HallId}, Cloud {config.CloudUrl}");
Console.WriteLine($"Bord http://localhost:{config.DeckPort}/bord, Matchuhr http://localhost:{config.DeckPort}/uhr");
app.Run();
return 0;
