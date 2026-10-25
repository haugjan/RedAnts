using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;

namespace RedAnts.Show.Agent.Deck;

public sealed record DeckSlot(int Number, string Label, string? ImageKey, int Color, int TextColor, bool Pressable, Func<Task>? Press, bool Active = false, string? MusicId = null, string Symbol = "", string Subtitle = "");

public sealed class Deck
{
    static readonly TimeSpan GoalWindow = TimeSpan.FromSeconds(25);

    const int CPanic = 0xFF0000;
    const int CBack = 0x8ED973;
    const int CNeutral = 0x555555;
    const int CEmpty = 0x000000;
    const int CWhite = 0xFFFFFF;
    const int CActive = 0xFFD400;
    const int CHead = 0x1A1A1A;
    const int CHome = 0xFF7C80;
    const int CAway = 0xA6C9EC;

    readonly PayloadStore _payload;
    readonly LiveGame _live;
    readonly ShowStates _states;
    readonly KeyRunner _runner;
    readonly ILogger<Deck> _log;
    readonly object _gate = new();
    readonly Dictionary<string, string> _page = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Flow> _flow = new(StringComparer.OrdinalIgnoreCase);

    TaskCompletionSource<bool> _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string _music = "";
    long _musicOrder;
    string _control = "";
    long _controlOrder;
    string _player = "";

    public Deck(PayloadStore payload, LiveGame live, ShowStates states, KeyRunner runner, ILogger<Deck> log)
    {
        _payload = payload;
        _live = live;
        _states = states;
        _runner = runner;
        _log = log;
        payload.Changed += Bump;
        states.Changed += Bump;
        runner.Changed += Bump;
        runner.MusicRequested += NoteMusic;
        runner.MusicStopped += () => NoteMusic(null);
        runner.MusicControl += NoteControl;
        live.Changed += Bump;
    }

    public long Version { get; private set; } = 1;

    public IReadOnlyList<string> Decks =>
        _payload.Hall.Decks is { Count: > 0 } decks ? decks.Select(d => d.Id).ToList() : [];

    public DeckLayout? Layout(string deckId) => _payload.Hall.DeckOf(deckId);

    public string MusicId => _music;

    public long MusicOrder => _musicOrder;

    public string ControlWanted => _control;

    public long ControlOrder => _controlOrder;

    public bool PlayedBy(string board) => _player.Length == 0 || _player == board;

    public void Claim(string board) => _player = board;

    public void NoteFromBoard(string? musicId)
    {
        if (_payload.MusicOf(musicId) is not { } music) return;
        if (string.Equals(_music, music.Id, StringComparison.OrdinalIgnoreCase)) _runner.Control("fade");
        else
        {
            NoteMusic(music);
            _states.Set(ShowStates.Music, "läuft", "Bord");
        }
    }

    public void NoteControl(string what)
    {
        lock (_gate)
        {
            _control = what;
            _controlOrder++;
            if (what is "fade" or "stop") _music = "";
        }
        Bump();
    }

    public void NoteMusic(MusicItem? music)
    {
        lock (_gate)
        {
            _music = music?.Id ?? "";
            _musicOrder++;
        }
        Bump();
    }

    public async Task<bool> WaitAsync(long since, TimeSpan timeout, CancellationToken stop)
    {
        Task<bool> waiter;
        lock (_gate)
        {
            if (Version != since) return true;
            waiter = _changed.Task;
        }
        var done = await Task.WhenAny(waiter, Task.Delay(timeout, stop));
        return done == waiter;
    }

    public object View(string deckId)
    {
        var deck = Layout(deckId);
        var page = CurrentPage(deckId);
        return new
        {
            version = Version,
            deck = deckId,
            page = page?.Id ?? "",
            title = Title(deckId),
            columns = deck?.Columns ?? 0,
            rows = deck?.Rows ?? 0,
            panic = deck?.Panic ?? 0,
            back = deck?.Back ?? 0,
            slots = Slots(deckId).Select(s => new
            {
                number = s.Number,
                label = s.Label,
                image = s.ImageKey,
                color = s.TextColor,
                bgcolor = s.Color,
                pressable = s.Pressable,
                active = s.Active,
                music = s.MusicId,
                symbol = s.Symbol,
                subtitle = s.Subtitle,
            }).ToArray(),
            live = Live(),
            states = _states.All,
        };
    }

    public object View(string deckId, long since) =>
        since > 0 && since == Version
            ? new { version = Version, live = Live(), states = _states.All }
            : View(deckId);

    public object LivePayload() => new { live = Live(), states = _states.All };

    object Live() => new
    {
        music = _music,
        musicOrder = _musicOrder,
        control = _control,
        controlOrder = _controlOrder,
        time = _live.Time,
        period = _live.Period,
        periodLabel = _live.PeriodLabel,
        home = _live.HomeScore,
        away = _live.AwayScore,
        running = _live.ClockRunning,
        clock = _live.ClockLive,
        spectators = _payload.Game.Spectators,
    };

    public string Title(string deckId)
    {
        if (_flow.TryGetValue(deckId, out var flow) && flow.Kind != FlowKind.None) return flow.Title;
        return CurrentPage(deckId)?.Name ?? Layout(deckId)?.Name ?? deckId;
    }

    public IReadOnlyList<DeckSlot> Slots(string deckId)
    {
        var deck = Layout(deckId);
        if (deck is null) return [];
        var builder = new Builder(deck.Columns, deck.Rows);
        var flow = _flow.TryGetValue(deckId, out var current) ? current : Flow.None;

        if (flow.Kind == FlowKind.Players) Players(builder, deck, deckId, flow);
        else if (flow.Kind == FlowKind.Penalty) Penalty(builder, deck, deckId, flow);
        else if (flow.Kind == FlowKind.Starting6) Starting6(builder, deck, deckId, flow);
        else if (flow.Kind == FlowKind.Spectators) Spectators(builder, deck, deckId);
        else Configured(builder, deck, deckId);

        Fixed(builder, deck, deckId, flow);
        builder.Fill(CEmpty);
        return builder.ToSlots();
    }

    public Task<string?> PressAsync(string deckId, int number)
    {
        var deck = Layout(deckId);
        if (deck is null) return Task.FromResult<string?>("Dieses Deck gibt es nicht");
        var slot = Slots(deckId).FirstOrDefault(s => s.Number == number);
        if (slot is null) return Task.FromResult<string?>($"Taste {number} gibt es nicht");
        if (!slot.Pressable || slot.Press is null) return Task.FromResult<string?>(null);
        _log.LogInformation("Deck {Deck}/{Page}: {Label}", deckId, Title(deckId), slot.Label.Replace('\n', ' '));
        return Run(slot.Press);
    }

    async Task<string?> Run(Func<Task> press)
    {
        try
        {
            await press();
            return null;
        }
        catch (Exception e)
        {
            _log.LogWarning("Taste gescheitert: {Message}", e.Message);
            return e.Message;
        }
    }

    public async Task PanicAsync(CancellationToken stop)
    {
        lock (_gate)
        {
            _page.Clear();
            _flow.Clear();
        }
        _runner.StopMusic();
        await Safe(() => _runner.LedStopAsync(stop));
        await Safe(() => _runner.HideAllAsync(withClock: true, stop));
        _states.Set(ShowStates.AutoBreakMusic, "aus", "PANIC");
        Bump();
    }

    async Task Safe(Func<Task> work)
    {
        try { await work(); }
        catch (Exception e) { _log.LogWarning("PANIC: {Message}", e.Message); }
    }

    public Task MusicControlAsync(string what)
    {
        switch (what)
        {
            case "stop": _runner.StopMusic(); break;
            case "fade": _runner.Control("fade"); break;
            case "next": _runner.Control("next"); break;
            case "previous": _runner.Control("previous"); break;
            case "pause": _runner.Control("pause"); break;
        }
        return Task.CompletedTask;
    }

    public void GoHome(string deckId) => Home(deckId);

    public Task ToggleMusicAsync(string? musicId)
    {
        if (_payload.MusicOf(musicId) is not { } music) return Task.CompletedTask;
        if (string.Equals(_music, music.Id, StringComparison.OrdinalIgnoreCase)) _runner.Control("fade");
        else _runner.PlayMusic(music.Id);
        return Task.CompletedTask;
    }

    DeckPage? CurrentPage(string deckId)
    {
        var deck = Layout(deckId);
        if (deck is null) return null;
        var id = _page.TryGetValue(deckId, out var stored) ? stored : DeckPage.HomeId;
        return deck.Page(id) ?? deck.Home;
    }

    void Configured(Builder builder, DeckLayout deck, string deckId)
    {
        var page = CurrentPage(deckId);
        if (page is null) return;
        foreach (var key in page.Keys)
        {
            if (deck.Reserved(key.Number, page.IsHome) || key.IsEmpty) continue;
            Key(builder, deck, deckId, key);
        }
    }

    void Key(Builder builder, DeckLayout deck, string deckId, DeckKey key)
    {
        var color = Rgb(key.Color);
        switch (key.Kind)
        {
            case KeyKinds.Music:
                var music = _payload.MusicOf(key.MusicId);
                if (music is null) { builder.Label(key.Number, key.Label, CNeutral); return; }
                var playing = string.Equals(_music, music.Id, StringComparison.OrdinalIgnoreCase);
                builder.Action(key.Number, key.Label.Length > 0 ? key.Label : music.Name, playing ? CActive : color,
                    () => ToggleMusicAsync(music.Id), Picture(key.Icon), playing, music.Id,
                    symbol: Symbol(key.Icon), subtitle: music.Subtitle);
                return;
            case KeyKinds.Steps:
                builder.Action(key.Number, key.Label, OnAir(key) ? CActive : color, () => StepsAsync(deckId, key),
                    Picture(key.Icon), OnAir(key), symbol: Symbol(key.Icon));
                return;
            case KeyKinds.Special:
                SpecialKeyOn(builder, deck, deckId, key);
                return;
            case KeyKinds.Page:
                builder.Action(key.Number, key.Label, color, () => { Go(deckId, key.PageId); return Task.CompletedTask; },
                    Picture(key.Icon), symbol: Symbol(key.Icon));
                return;
            case KeyKinds.Live:
                builder.Label(key.Number, LiveText(key), color);
                return;
            default:
                return;
        }
    }

    string LiveText(DeckKey key) => key.Live switch
    {
        "zeit" => _live.Time,
        "drittel" => _live.PeriodLabel.Length > 0 ? _live.PeriodLabel : "Drittel",
        "stand.heim" => _live.HomeScore,
        "stand.gast" => _live.AwayScore,
        "einblender" => _runner.OnAir.Length > 0 ? _runner.OnAir : key.Label,
        _ => key.Label,
    };

    void SpecialKeyOn(Builder builder, DeckLayout deck, string deckId, DeckKey key)
    {
        var special = key.Special ?? "";
        var color = Rgb(key.Color);
        var label = key.Label.Length > 0 ? key.Label : special;

        if (DefaultStateOf(special) is { } stateId)
        {
            var state = _states.Value(stateId);
            var on = state.Length > 0 && !state.Equals(_states.FirstValue(stateId), StringComparison.OrdinalIgnoreCase);
            builder.Action(key.Number, $"{label}\n{state}", on ? 0xC0201C : 0x1E7B34,
                () => ToggleStateAsync(special, stateId), key.Icon, forceText: CWhite);
            return;
        }

        if (special is SpecialKeys.Starting6Home or SpecialKeys.Starting6Away)
        {
            var side = SpecialKeys.Side(special);
            if (!_payload.Game.Side(side).HasStarting6)
            {
                builder.Label(key.Number, $"{label}\n(unvollständig)", CNeutral);
                return;
            }
        }

        builder.Action(key.Number, label, color, () => SpecialAsync(deckId, key, special), Picture(key.Icon), symbol: Symbol(key.Icon));
    }

    static string? DefaultStateOf(string special) => special switch
    {
        SpecialKeys.OverlayToggle => ShowStates.Overlay,
        SpecialKeys.ClockToggle => ShowStates.ClockVisible,
        SpecialKeys.MusicToggle => ShowStates.Music,
        SpecialKeys.AutoBreakMusic => ShowStates.AutoBreakMusic,
        _ => null,
    };

    async Task ToggleStateAsync(string special, string stateId)
    {
        var value = _states.Value(stateId);
        var on = value.Length > 0 && !value.Equals(_states.FirstValue(stateId), StringComparison.OrdinalIgnoreCase);
        switch (special)
        {
            case SpecialKeys.OverlayToggle:
                if (on) await _runner.HideAllAsync(CancellationToken.None);
                else if (_runner.Shown is { Length: > 0 } last) await _runner.ShowTitleAsync(last, KeyContext.None, CancellationToken.None);
                break;
            case SpecialKeys.ClockToggle:
                if (on) await _runner.HideTitleAsync("scoreboard", CancellationToken.None);
                else await _runner.ShowTitleAsync("scoreboard", KeyContext.None, CancellationToken.None);
                break;
            case SpecialKeys.MusicToggle:
                if (on) _runner.StopMusic();
                break;
            case SpecialKeys.AutoBreakMusic:
                _states.Set(ShowStates.AutoBreakMusic, on ? "aus" : "an", "Taste");
                break;
        }
        Bump();
    }

    bool OnAir(DeckKey key)
    {
        if (TitleOf(key) is not { } kind || _runner.Shown.Length == 0) return false;
        if (!_runner.Shown.Equals(kind, StringComparison.OrdinalIgnoreCase)) return false;
        var side = SideOf(key);
        return side.Length == 0 || _runner.ShownSide.Equals(side, StringComparison.OrdinalIgnoreCase);
    }

    static string? TitleOf(DeckKey key) =>
        key.Sequence.FirstOrDefault(step => step.Action == KeyActions.VmixShow) is { Target: { Length: > 0 } target }
            ? target.StartsWith(Keys.KeyRunner.TitlePrefix, StringComparison.OrdinalIgnoreCase)
                ? target[Keys.KeyRunner.TitlePrefix.Length..]
                : target
            : null;

    async Task StepsAsync(string deckId, DeckKey key)
    {
        if (OnAir(key) && TitleOf(key) is { } live)
        {
            await Safe(() => _runner.HideTitleAsync(live, CancellationToken.None));
            return;
        }
        var ctx = new KeyContext(SideOf(key));
        if (key.Sequence.Any(step => step.Action == KeyActions.Wait))
            _ = Safe(() => _runner.RunAsync(key.Sequence, ctx, CancellationToken.None));
        else
            await _runner.RunAsync(key.Sequence, ctx, CancellationToken.None);
        if (key.HomeAfter) Go(deckId, DeckPage.HomeId);
    }

    static string SideOf(DeckKey key)
    {
        foreach (var step in key.Sequence)
        {
            var target = step.Target ?? "";
            if (target.EndsWith("_away", StringComparison.OrdinalIgnoreCase) || target.EndsWith("-gast", StringComparison.OrdinalIgnoreCase)) return "away";
            if (target.EndsWith("_home", StringComparison.OrdinalIgnoreCase) || target.EndsWith("-heim", StringComparison.OrdinalIgnoreCase)) return "home";
        }
        return "";
    }

    async Task SpecialAsync(string deckId, DeckKey key, string special)
    {
        var side = SpecialKeys.Side(special);
        switch (special)
        {
            case SpecialKeys.GoalHome:
            case SpecialKeys.GoalAway:
                await _runner.RunAsync(key.Sequence, new KeyContext(side), CancellationToken.None);
                Open(deckId, new Flow(FlowKind.Players, side, "goal", $"Tor {Team(side)}", key));
                break;
            case SpecialKeys.PenaltyHome:
            case SpecialKeys.PenaltyAway:
                await _runner.RunAsync(key.Sequence, new KeyContext(side), CancellationToken.None);
                Open(deckId, new Flow(FlowKind.Penalty, side, "penalty", $"Strafe {Team(side)}", key));
                break;
            case SpecialKeys.BestPlayerHome:
            case SpecialKeys.BestPlayerAway:
                await _runner.RunAsync(key.Sequence, new KeyContext(side), CancellationToken.None);
                Open(deckId, new Flow(FlowKind.Players, side, "bestplayer", $"Best Player {Team(side)}", key));
                break;
            case SpecialKeys.PlayerHome:
            case SpecialKeys.PlayerAway:
                Open(deckId, new Flow(FlowKind.Players, side, "name", $"Spielerin {Team(side)}", key));
                break;
            case SpecialKeys.Starting6Home:
            case SpecialKeys.Starting6Away:
                await _runner.RunAsync(key.Sequence, new KeyContext(side), CancellationToken.None);
                Open(deckId, new Flow(FlowKind.Starting6, side, "starting6", $"Starting 6 {Team(side)}", key));
                break;
            case SpecialKeys.MusicStop:
                _runner.StopMusic();
                break;
            case SpecialKeys.MusicFade:
                _runner.Control("fade");
                break;
            case SpecialKeys.MusicNext:
                _runner.Control("next");
                break;
            case SpecialKeys.MusicPrevious:
                _runner.Control("previous");
                break;
            case SpecialKeys.MusicPause:
                _runner.Control("pause");
                break;
            case SpecialKeys.Spectators:
                Open(deckId, new Flow(FlowKind.Spectators, "", "", "Zuschauer", key));
                break;
        }
        Bump();
    }

    string Team(string side) =>
        _payload.Game.Side(side).ShortName is { Length: > 0 } name ? name : side == "away" ? "Gast" : "Heim";

    void Players(Builder builder, DeckLayout deck, string deckId, Flow flow)
    {
        var team = _payload.Game.Side(flow.Side);
        var color = flow.Side == "away" ? CAway : CHome;
        var places = Places(deck).ToList();
        var index = 0;
        var room = places.Count - (flow.Event == "goal" ? 3 : 2);
        foreach (var player in team.Players)
        {
            if (index >= room) break;
            var number = places[index++];
            var chosen = player;
            builder.Action(number, $"{chosen.Display}\n{Short(chosen.Name)}", color, () => PickAsync(deckId, flow, chosen));
        }
        if (places.Count > 0)
            builder.Label(places[^1], _runner.OnAir.Length > 0 ? _runner.OnAir : flow.Title, CHead);
        if (flow.Event == "goal" && index < places.Count)
            builder.Action(places[index++], "Eigentor", color, () => PickAsync(deckId, flow, null, own: true));
        if (team.TopScorerPlayer is { } top && index < places.Count)
            builder.Action(places[index], $"Topscorer\n{Short(top.Name)}", color, () => PickAsync(deckId, flow, top));
    }

    void Penalty(Builder builder, DeckLayout deck, string deckId, Flow flow)
    {
        var color = flow.Side == "away" ? CAway : CHome;
        var places = Places(deck).ToList();
        var options = new[] { ("2'", "2"), ("2'+2'", "22"), ("10'", "10"), ("Spieldauer", "match") };
        for (var i = 0; i < options.Length && i < places.Count; i++)
        {
            var (label, value) = options[i];
            builder.Action(places[i], label, color, () =>
            {
                Open(deckId, flow with { Kind = FlowKind.Players, Penalty = value });
                return Task.CompletedTask;
            });
        }
    }

    void Starting6(Builder builder, DeckLayout deck, string deckId, Flow flow)
    {
        var team = _payload.Game.Side(flow.Side);
        var color = flow.Side == "away" ? CAway : CHome;
        var places = Places(deck).ToList();
        var six = team.Starting6Players;
        for (var i = 0; i < six.Count && i < places.Count - 1; i++)
        {
            var player = six[i];
            var shown = flow.Position == i + 1;
            builder.Action(places[i], $"{player.Display}\n{Short(player.Name)}", shown ? CActive : color,
                () => ShowSixAsync(deckId, flow, i + 1));
        }
        if (places.Count > 0)
            builder.Action(places[^1], "▶▶ Weiter", CBack, () => ShowSixAsync(deckId, flow, flow.Position + 1));
    }

    void Spectators(Builder builder, DeckLayout deck, string deckId)
    {
        var places = Places(deck).ToList();
        var steps = new[] { ("+100", 100), ("+10", 10), ("+1", 1), ("-1", -1), ("-10", -10), ("-100", -100) };
        for (var i = 0; i < steps.Length && i < places.Count - 1; i++)
        {
            var (label, delta) = steps[i];
            builder.Action(places[i], label, CNeutral, () =>
            {
                _payload.SetSpectators(Math.Max(0, _payload.Game.Spectators + delta));
                return Task.CompletedTask;
            });
        }
        if (places.Count > 0)
            builder.Action(places[^1], $"👁 {_payload.Game.Spectators}\nanzeigen", CBack, async () =>
            {
                await _runner.ShowTitleAsync("spectators", new KeyContext(Text: _payload.Game.Spectators.ToString()), CancellationToken.None);
                Home(deckId);
            });
    }

    async Task PickAsync(string deckId, Flow flow, Player? player, bool own = false)
    {
        var ctx = new KeyContext(flow.Side, player, flow.Penalty, Event: own ? "owngoal" : flow.Event);
        if (flow.Event == "goal") ctx = ctx with { Score = _live.GoalScore(flow.Side, GoalWindow) };
        var kind = flow.Event switch
        {
            "goal" => own ? "player_event" : "player_event",
            "penalty" => "penalty",
            "bestplayer" => "bestplayer",
            _ => "name",
        };
        Home(deckId);
        await _runner.ShowTitleAsync(kind, ctx, CancellationToken.None);
        await _runner.ShowPlayerOnLedAsync(ctx, CancellationToken.None);
    }

    async Task ShowSixAsync(string deckId, Flow flow, int position)
    {
        var team = _payload.Game.Side(flow.Side);
        var six = team.Starting6Players;
        if (position > six.Count)
        {
            await _runner.HideTitleAsync("starting6", CancellationToken.None);
            Home(deckId);
            return;
        }
        Set(deckId, flow with { Position = position });
        await _runner.ShowTitleAsync("starting6", new KeyContext(flow.Side, six[position - 1], Event: position.ToString()), CancellationToken.None);
    }

    void Fixed(Builder builder, DeckLayout deck, string deckId, Flow flow)
    {
        var page = CurrentPage(deckId);
        var deep = flow.Kind != FlowKind.None || (page is not null && !page.IsHome);
        if (deep) builder.Action(deck.Back, "◀ Zurück", CBack, () => { Back(deckId); return Task.CompletedTask; });
        else if (page is null || page.KeyAt(deck.Back).IsEmpty) builder.Empty(deck.Back);
        builder.Action(deck.Panic, "🚨 PANIC", CPanic, () => PanicAsync(CancellationToken.None), forceText: CWhite);
    }

    IEnumerable<int> Places(DeckLayout deck)
    {
        for (var number = 1; number <= deck.Count; number++)
            if (!deck.Reserved(number)) yield return number;
    }

    static string Short(string name)
    {
        if (name.Length <= 12) return name;
        var space = name.IndexOf(' ');
        if (space < 1) return name[..12];
        var first = name[..space];
        var family = name[(space + 1)..].TrimStart();
        var shortened = family.Length > 0 ? $"{first} {family[0]}." : first;
        return shortened.Length <= 12 ? shortened : first[..Math.Min(first.Length, 12)];
    }

    void Open(string deckId, Flow flow)
    {
        Set(deckId, flow);
        Bump();
    }

    void Set(string deckId, Flow flow)
    {
        lock (_gate) _flow[deckId] = flow;
    }

    void Go(string deckId, string? pageId)
    {
        lock (_gate)
        {
            _flow.Remove(deckId);
            if (pageId is { Length: > 0 }) _page[deckId] = pageId;
        }
        Bump();
    }

    void Home(string deckId) => Go(deckId, DeckPage.HomeId);

    void Back(string deckId)
    {
        lock (_gate)
        {
            if (_flow.Remove(deckId)) { }
            else if (_page.TryGetValue(deckId, out var current) && Layout(deckId)?.Page(current)?.ParentId is { } parent)
                _page[deckId] = parent;
            else _page[deckId] = DeckPage.HomeId;
        }
        Bump();
    }

    void Bump()
    {
        TaskCompletionSource<bool> waiting;
        lock (_gate)
        {
            Version++;
            waiting = _changed;
            _changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        waiting.TrySetResult(true);
    }

    static string? Picture(string? icon) =>
        icon is { Length: > 0 } && DeckIcons.Known(icon) ? icon : null;

    static string Symbol(string? icon) =>
        icon is { Length: > 0 } && !DeckIcons.Known(icon) ? icon : "";

    static int Rgb(string? color)
    {
        if (color is not { Length: 7 } hex || hex[0] != '#') return CNeutral;
        return int.TryParse(hex[1..], System.Globalization.NumberStyles.HexNumber, null, out var value) ? value : CNeutral;
    }

    enum FlowKind { None, Players, Penalty, Starting6, Spectators }

    sealed record Flow(FlowKind Kind, string Side, string Event, string Title, DeckKey? Key, string Penalty = "", int Position = 0)
    {
        public static readonly Flow None = new(FlowKind.None, "", "", "", null);
    }

    sealed class Builder(int columns, int rows)
    {
        readonly Dictionary<int, DeckSlot> _slots = [];

        int Count => columns * rows;

        public void Action(int number, string label, int bg, Func<Task> press, string? icon = null, bool active = false, string? musicId = null, int? forceText = null, string symbol = "", string subtitle = "")
        {
            if (number < 1 || number > Count) return;
            var fg = forceText ?? Fg(bg);
            _slots[number] = new DeckSlot(number, label, icon is null ? null : DeckIcons.Key(icon, label, fg), bg, fg, true, press, active, musicId, symbol, subtitle);
        }

        public void Label(int number, string label, int bg)
        {
            if (number < 1 || number > Count) return;
            _slots[number] = new DeckSlot(number, label, null, bg, Fg(bg), false, null);
        }

        public void Empty(int number)
        {
            if (number < 1 || number > Count) return;
            _slots[number] = new DeckSlot(number, "", null, CEmpty, CWhite, false, null);
        }

        public void Fill(int color)
        {
            for (var n = 1; n <= Count; n++)
                if (!_slots.ContainsKey(n)) _slots[n] = new DeckSlot(n, "", null, color, Fg(color), false, null);
        }

        public IReadOnlyList<DeckSlot> ToSlots() =>
            Enumerable.Range(1, Count).Select(n => _slots.TryGetValue(n, out var slot) ? slot : new DeckSlot(n, "", null, CEmpty, CWhite, false, null)).ToList();

        static int Fg(int bg)
        {
            var r = (bg >> 16) & 0xFF;
            var g = (bg >> 8) & 0xFF;
            var b = bg & 0xFF;
            return r * 299 + g * 587 + b * 114 > 140000 ? CHead : CWhite;
        }
    }
}
