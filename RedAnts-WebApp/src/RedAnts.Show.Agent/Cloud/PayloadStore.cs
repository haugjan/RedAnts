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

    public IReadOnlyList<MusicItem> Music => Current.Music ?? [];

    public MusicItem? MusicOf(string? id) =>
        id is { Length: > 0 } ? Music.FirstOrDefault(m => m.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) : null;

    public bool Synced => Current.Hall.Id.Length > 0;

    public void Apply(AgentPayload payload)
    {
        lock (_gate)
        {
            payload = Kept(_current, payload);
            _current = payload;
        }
        Save(payload);
        log.LogInformation("Konfiguration übernommen: Halle {Hall}, {Decks} Decks, {Music} Musik-Befehle, Spiel {Game}",
            payload.Hall.Name, payload.Hall.Decks?.Count ?? 0, payload.Music?.Count ?? 0,
            payload.Game?.Title is { Length: > 0 } title ? title : "keines");
        Changed?.Invoke();
    }

    public void ApplyGame(Game game)
    {
        AgentPayload next;
        lock (_gate)
        {
            next = _current with { Game = game };
            _current = next;
        }
        Save(next);
        Changed?.Invoke();
    }

    public void SetSpectators(int count)
    {
        AgentPayload next;
        lock (_gate)
        {
            if (_current.Game is not { } game || game.Spectators == count) return;
            next = _current with { Game = game with { Spectators = count } };
            _current = next;
        }
        Save(next);
        Changed?.Invoke();
    }

    static AgentPayload Kept(AgentPayload current, AgentPayload fresh)
    {
        if (current.Game is not { } local || fresh.Game is not { Id.Length: > 0 } coming || local.Id != coming.Id) return fresh;
        return local.LoadedAt > coming.LoadedAt
            ? fresh with { Game = local }
            : fresh with { Game = coming with { Spectators = local.Spectators } };
    }

    void Save(AgentPayload payload)
    {
        try
        {
            Directory.CreateDirectory(AgentConfig.DataDir);
            System.IO.File.WriteAllText(File, JsonSerializer.Serialize(payload, PayloadJson.Pretty));
        }
        catch (IOException e) { log.LogWarning("Konfiguration konnte nicht gesichert werden: {Message}", e.Message); }
    }

    static AgentPayload Load(ILogger log)
    {
        var empty = new AgentPayload(Hall.Offline, null, DateTimeOffset.MinValue);
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
