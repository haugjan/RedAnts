using System.Text.Json;

namespace RedAnts.Show.Agent.Cloud;

public sealed class GameOutbox(ILogger<GameOutbox> log)
{
    static string File => Path.Combine(AgentConfig.DataDir, "game-outbox.json");

    readonly object _gate = new();
    Game? _pending = Load(log);

    public event Action? Changed;

    public Game? Pending
    {
        get { lock (_gate) return _pending; }
    }

    public void Queue(Game game)
    {
        lock (_gate) _pending = game;
        Save(game);
        Changed?.Invoke();
    }

    public void Sent(Game game)
    {
        lock (_gate)
        {
            if (_pending is not { } pending || pending.LoadedAt > game.LoadedAt) return;
            _pending = null;
        }
        Save(null);
        Changed?.Invoke();
    }

    void Save(Game? game)
    {
        try
        {
            Directory.CreateDirectory(AgentConfig.DataDir);
            if (game is null) System.IO.File.Delete(File);
            else System.IO.File.WriteAllText(File, JsonSerializer.Serialize(game, PayloadJson.Pretty));
        }
        catch (IOException e) { log.LogWarning("Spiel konnte nicht zwischengelegt werden: {Message}", e.Message); }
    }

    static Game? Load(ILogger log)
    {
        if (!System.IO.File.Exists(File)) return null;
        try
        {
            var stored = JsonSerializer.Deserialize<Game>(System.IO.File.ReadAllText(File), PayloadJson.Options);
            if (stored is not null) log.LogInformation("Spiel {Game} wartet noch auf die Cloud", stored.Id);
            return stored;
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            log.LogWarning("Zwischengelegtes Spiel unlesbar: {Message}", e.Message);
            return null;
        }
    }
}
