using System.Text.Json;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Deck;

public sealed record TabState(bool Active = false, string League = "", string Tab = "");

public sealed class TabSession
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    readonly PayloadStore _payload;
    readonly string? _file;
    readonly object _gate = new();
    TabState _state;

    public TabSession(PayloadStore payload) : this(payload, Path.Combine(AgentConfig.DataDir, "tabs.json"))
    {
    }

    public TabSession(PayloadStore payload, string? file)
    {
        _payload = payload;
        _file = file;
        _state = Load(file);
    }

    public event Action? Changed;

    public TabBoardPayload? Board => _payload.Current.Tabs is { Variants.Count: > 0 } board ? board : null;

    public bool Available => Board is not null;

    public bool Active
    {
        get { lock (_gate) return _state.Active && Available; }
    }

    public string League
    {
        get
        {
            var wanted = State.League;
            return Board is { } board && board.Leagues.Any(l => l.Id == wanted) ? wanted : Board?.DefaultLeague ?? "";
        }
    }

    public string Tab
    {
        get
        {
            var wanted = State.Tab;
            return Board is { } board && board.Tabs.Any(t => t.Id == wanted) ? wanted : Board?.Tabs.FirstOrDefault()?.Id ?? "";
        }
    }

    public string LeagueName => Board?.Leagues.FirstOrDefault(l => l.Id == League)?.Name ?? League;

    public TabState State
    {
        get { lock (_gate) return _state; }
    }

    public void Start(string league) => Set(new TabState(true, league, Board?.Tabs.FirstOrDefault()?.Id ?? ""));

    public void Stop() => Set(State with { Active = false });

    public void Choose(string tab) => Set(State with { Tab = tab });

    public void ChooseLeague(string league) => Set(State with { League = league });

    public DeckLayout? Variant(string deckId)
    {
        if (!Active || Board is not { } board) return null;
        var league = League;
        var tab = Tab;
        return board.Variants.FirstOrDefault(v =>
            v.DeckId.Equals(deckId, StringComparison.OrdinalIgnoreCase) && v.League == league && v.Tab == tab)?.Layout;
    }

    void Set(TabState next)
    {
        lock (_gate)
        {
            if (_state == next) return;
            _state = next;
        }
        Save(next);
        Changed?.Invoke();
    }

    void Save(TabState state)
    {
        if (_file is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
            File.WriteAllText(_file, JsonSerializer.Serialize(state, Json));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    static TabState Load(string? file)
    {
        try
        {
            return file is not null && File.Exists(file)
                ? JsonSerializer.Deserialize<TabState>(File.ReadAllText(file), Json) ?? new TabState()
                : new TabState();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return new TabState();
        }
    }
}
