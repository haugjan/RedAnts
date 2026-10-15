using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using RedAnts.Show.Agent.Scenes;

namespace RedAnts.Show.Agent.Cloud;

public sealed class CloudLink(AgentConfig config, PayloadStore payload, SceneRunner runner, Deck.Deck deck, ILogger<CloudLink> log) : BackgroundService
{
    static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    HubConnection? _hub;

    public bool Connected => _hub?.State == HubConnectionState.Connected;

    public string? Fault { get; private set; }

    public DateTimeOffset? ConnectedAt { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var hub = new HubConnectionBuilder()
            .WithUrl($"{config.HubUrl}?hall={Uri.EscapeDataString(config.HallId)}&key={Uri.EscapeDataString(config.Key)}&version={Uri.EscapeDataString(AgentConfig.Version)}")
            .WithAutomaticReconnect(new EndlessRetry())
            .Build();
        hub.On<AgentPayload>("Sync", payload.Apply);
        hub.On<string, Dictionary<string, string>>("Run", (sceneId, args) => runner.Start(sceneId, args));
        hub.On("Panic", () => runner.Start(SceneRunner.PanicScene, new Dictionary<string, string>()));
        hub.On<string, int>("Press", async (deckId, number) => await deck.PressAsync(deckId, number));
        hub.On<string>("Sound", async soundId => await deck.ToggleAsync(soundId));
        hub.On<string, object>("View", deckId => deck.View(deckId));
        hub.On<string, long, object>("ViewSince", (deckId, since) => deck.View(deckId, since));
        hub.Reconnecting += e =>
        {
            Fault = e?.Message ?? "Verbindung unterbrochen";
            log.LogWarning("Cloud-Verbindung unterbrochen, verbinde neu: {Message}", Fault);
            return Task.CompletedTask;
        };
        hub.Reconnected += _ =>
        {
            Fault = null;
            ConnectedAt = DateTimeOffset.Now;
            log.LogInformation("Cloud wieder verbunden");
            return Task.CompletedTask;
        };
        hub.Closed += e =>
        {
            Fault = e?.Message ?? "Verbindung geschlossen";
            ConnectedAt = null;
            return Task.CompletedTask;
        };
        _hub = hub;

        while (!stop.IsCancellationRequested)
        {
            if (hub.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await hub.StartAsync(stop);
                    Fault = null;
                    ConnectedAt = DateTimeOffset.Now;
                    log.LogInformation("Cloud {Url} verbunden als Halle {Hall}", config.CloudUrl, config.HallId);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception e)
                {
                    Fault = e.Message;
                    log.LogWarning("Cloud {Url} nicht erreichbar: {Message}", config.CloudUrl, e.Message);
                }
            }
            try { await Task.Delay(RetryDelay, stop); }
            catch (OperationCanceledException) { break; }
        }

        await hub.DisposeAsync();
    }

    public async Task SendAsync(string method, CancellationToken stop, params object?[] args)
    {
        if (_hub is not { State: HubConnectionState.Connected } hub) return;
        try { await hub.SendCoreAsync(method, args, stop); }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogDebug("{Method} nicht zugestellt: {Message}", method, e.Message);
        }
    }

    public async Task ReportAsync(string kind, object body, CancellationToken stop)
    {
        if (_hub is not { State: HubConnectionState.Connected } hub) return;
        try { await hub.SendAsync("Report", kind, JsonSerializer.Serialize(body, PayloadJson.Options), stop); }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogDebug("Meldung {Kind} nicht zugestellt: {Message}", kind, e.Message);
        }
    }

    sealed class EndlessRetry : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext context) =>
            context.PreviousRetryCount < 3 ? TimeSpan.FromSeconds(2) : RetryDelay;
    }
}
