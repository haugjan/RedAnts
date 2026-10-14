using System.Text.Json;

namespace RedAnts.Show.Agent.Cloud;

public sealed class PayloadStore(ILogger<PayloadStore> log)
{
    static string File => Path.Combine(AgentConfig.DataDir, "payload.json");

    readonly object _gate = new();
    AgentPayload _current = Load(log);

    public event Action? Changed;

    public AgentPayload Current
    {
        get { lock (_gate) return _current; }
    }

    public Hall Hall => Current.Hall;

    public Game Game => Current.Game ?? Game.None;

    public IReadOnlyList<Scene> Scenes => Current.Scenes;

    public IReadOnlyList<SoundCommand> Sounds => Current.Sounds ?? [];

    public SoundCommand? Sound(string? id) =>
        id is { Length: > 0 } ? Sounds.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) : null;

    public bool Synced => Current.Hall.Id.Length > 0;

    public Scene? Scene(string id) =>
        Scenes.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public void Apply(AgentPayload payload)
    {
        lock (_gate) _current = payload;
        try
        {
            Directory.CreateDirectory(AgentConfig.DataDir);
            System.IO.File.WriteAllText(File, JsonSerializer.Serialize(payload, PayloadJson.Pretty));
        }
        catch (IOException e) { log.LogWarning("Konfiguration konnte nicht gesichert werden: {Message}", e.Message); }
        log.LogInformation("Konfiguration übernommen: Halle {Hall}, {Scenes} Szenen, {Sounds} Musik-Befehle, Spiel {Game}",
            payload.Hall.Name, payload.Scenes.Count, payload.Sounds?.Count ?? 0, payload.Game?.Title is { Length: > 0 } title ? title : "keines");
        Changed?.Invoke();
    }

    static AgentPayload Load(ILogger log)
    {
        var empty = new AgentPayload(Hall.Offline, [], null, DateTimeOffset.MinValue);
        if (!System.IO.File.Exists(File)) return empty;
        try
        {
            var stored = JsonSerializer.Deserialize<AgentPayload>(System.IO.File.ReadAllText(File), PayloadJson.Options);
            if (stored is not null) log.LogInformation("Gesicherte Konfiguration vom {SentAt:dd.MM.yyyy HH:mm} geladen", stored.SentAt);
            return stored ?? empty;
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            log.LogWarning("Gesicherte Konfiguration unlesbar: {Message}", e.Message);
            return empty;
        }
    }
}
