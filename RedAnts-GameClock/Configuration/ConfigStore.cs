using System.Text.Json;
using Microsoft.Extensions.Options;

namespace RedAnts.GameClock.Configuration;

public sealed class ConfigStore
{
    static readonly JsonSerializerOptions Format = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    readonly string _file;
    readonly ILogger<ConfigStore> _log;
    readonly SemaphoreSlim _write = new(1, 1);

    public ConfigStore(IOptions<GameClockOptions> seed, DataFolder data, ILogger<ConfigStore> log)
    {
        _file = Path.Combine(data.Root, "gameclock.json");
        _log = log;
        Current = Load(seed.Value);
    }

    public GameClockConfig Current { get; private set; }

    public string File => _file;

    public event Action? Changed;

    public async Task SaveAsync(GameClockConfig config)
    {
        config.Configured = true;
        await _write.WaitAsync();
        try
        {
            await System.IO.File.WriteAllTextAsync(_file, JsonSerializer.Serialize(config, Format));
            Current = config;
        }
        finally { _write.Release(); }
        Changed?.Invoke();
    }

    GameClockConfig Load(GameClockOptions seed)
    {
        if (System.IO.File.Exists(_file))
        {
            try
            {
                var stored = JsonSerializer.Deserialize<GameClockConfig>(System.IO.File.ReadAllText(_file), Format);
                if (stored is not null) return stored;
                _log.LogWarning("Konfiguration {File} ist leer, Vorgaben werden verwendet", _file);
            }
            catch (JsonException e)
            {
                _log.LogWarning("Konfiguration {File} ist unlesbar ({Message}), Vorgaben werden verwendet", _file, e.Message);
            }
        }
        return FromSeed(seed);
    }

    static GameClockConfig FromSeed(GameClockOptions seed)
    {
        var config = new GameClockConfig
        {
            Clock = new ClockSourceConfig { Port = seed.Port, SourceIp = seed.Ip },
            Teams = new TeamConfig { Home = new TeamEntry { Name = seed.Home.Name, Logo = seed.Home.Logo } },
        };
        foreach (var (abbreviation, team) in seed.Guests)
            config.Teams.ByAbbreviation[abbreviation] = new TeamEntry { Name = team.Name, Logo = team.Logo };
        return config;
    }
}

public sealed class DataFolder
{
    public DataFolder(IOptions<GameClockOptions> options, IHostEnvironment env)
    {
        var configured = options.Value.DataDir;
        Root = Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured);
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Logos);
    }

    public string Root { get; }

    public string Logos => Path.Combine(Root, "logos");

    public string Cache(string name) => Path.Combine(Root, name);
}
