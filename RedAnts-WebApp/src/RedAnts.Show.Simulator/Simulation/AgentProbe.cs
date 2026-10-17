using System.Text.Json;

namespace RedAnts.Show.Simulator.Simulation;

public sealed class AgentProbe(EventLog log, ClockSender clock) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };
    private string _lastSummary = "";

    public string AgentUrl { get; set; } = "http://localhost:5150";
    public bool Reachable { get; private set; }
    public IReadOnlyDictionary<string, string> Status { get; private set; } = new Dictionary<string, string>();
    public string? DeckContext { get; private set; }
    public string? Scene { get; private set; }

    public event Action? Changed;

    public async Task<string?> PressAsync(int key)
    {
        try
        {
            using var response = await _http.PostAsync($"{AgentUrl.TrimEnd('/')}/deck/press/{key}", null);
            var body = await response.Content.ReadAsStringAsync();
            log.Add("Agent", $"Taste {key}: {body}");
            return body;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            log.Add("Agent", $"Taste {key}: {e.Message}");
            return e.Message;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            await ProbeAsync(stop);
            try { await Task.Delay(Interval, stop); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ProbeAsync(CancellationToken stop)
    {
        try
        {
            using var document = JsonDocument.Parse(await _http.GetStringAsync($"{AgentUrl.TrimEnd('/')}/status", stop));
            var status = new Dictionary<string, string>();
            foreach (var property in document.RootElement.EnumerateObject())
                status[property.Name] = property.Value.ValueKind == JsonValueKind.Null ? "" : property.Value.ToString();
            Status = status;
            if (status.TryGetValue("clockListening", out var listening)
                && int.TryParse(new string(listening.Where(char.IsDigit).ToArray()), out var port))
                clock.AdoptPort(port);
            DeckContext = status.GetValueOrDefault("deck");
            Scene = status.GetValueOrDefault("scene");
            if (!Reachable) log.Add("Agent", $"Agent unter {AgentUrl} erreichbar");
            Reachable = true;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            if (Reachable) log.Add("Agent", $"Agent nicht erreichbar: {e.Message}");
            Reachable = false;
            Status = new Dictionary<string, string>();
        }
        var summary = string.Join('|', Status.Select(kv => kv.Key + "=" + kv.Value)) + Reachable;
        if (summary == _lastSummary) return;
        _lastSummary = summary;
        Changed?.Invoke();
    }
}
