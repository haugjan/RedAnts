using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;
using RedAnts.Show.Agent.Led;

namespace RedAnts.Show.Agent.Deck;

public sealed record DeckSlot(int Number, string Label, string? ImageKey, int Color, int TextColor, bool Pressable, Func<Task>? Press, bool Active = false, string? MusicId = null, string Symbol = "", string Subtitle = "", string Top = "");

public sealed class Deck
{
    static readonly TimeSpan GoalWindow = TimeSpan.FromSeconds(25);
    static readonly TimeSpan SpeakerTimeout = TimeSpan.FromMinutes(3);
    static readonly TimeSpan FaultShown = TimeSpan.FromSeconds(20);

    const int CPanic = 0xFF0000;
    const int CBack = 0x1E7B34;
    const int CNeutral = 0x555555;
    const int CEmpty = 0x000000;
    const int CWhite = 0xFFFFFF;
    const int CActive = 0xFFD400;
    const int CHead = 0x1A1A1A;
    const int CHome = 0xFF7C80;
    const int CAway = 0xA6C9EC;
    const int CAssist = 0xFF8C1A;

    readonly PayloadStore _payload;
    readonly LiveGame _live;
    readonly ShowStates _states;
    readonly KeyRunner _runner;
    readonly ILogger<Deck> _log;
    readonly object _gate = new();
    readonly Dictionary<string, List<Step>> _trail = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Flow> _flow = new(StringComparer.OrdinalIgnoreCase);

    TaskCompletionSource<bool> _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string _music = "";
    long _musicOrder;
    string _control = "";
    long _controlOrder;
    string _player = "";
    string? _fault;
    DateTimeOffset _faultAt;

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
        runner.Failed += NoteFault;
        live.Changed += Bump;
    }

    public long Version { get; private set; } = 1;

    public IReadOnlyList<string> Decks =>
        _payload.Hall.Decks is { Count: > 0 } decks ? decks.Select(d => d.Id).ToList() : [];

    public DeckLayout? Layout(string deckId) => _payload.Hall.DeckOf(deckId);

    public string Resolve(string? deckId) => _payload.Hall.ResolveDeck(deckId);

    public string? RecentFault =>
        _fault is { } fault && DateTimeOffset.UtcNow - _faultAt < FaultShown ? fault : null;

    void NoteFault(string message)
    {
        lock (_gate)
        {
            _fault = message;
            _faultAt = DateTimeOffset.UtcNow;
        }
        Bump();
    }

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
                top = s.Top,
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
        if (Trail(deckId).LastOrDefault() is { Title.Length: > 0 } step) return step.Title;
        return CurrentPage(deckId)?.Name ?? Layout(deckId)?.Name ?? deckId;
    }

    public IReadOnlyList<DeckSlot> Slots(string deckId)
    {
        var deck = Layout(deckId);
        if (deck is null) return [];
        var builder = new Builder(deck.Columns, deck.Rows);
        var flow = _flow.TryGetValue(deckId, out var current) ? current : Flow.None;

        if (flow.Kind == FlowKind.Players) Players(builder, deck, deckId, flow);
        else if (flow.Kind == FlowKind.Assist) Assists(builder, deck, deckId, flow);
        else if (flow.Kind == FlowKind.Speaker) Speaker(builder, deck, deckId, flow);
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
            NoteFault(e.Message);
            return e.Message;
        }
    }

    public async Task PanicAsync(CancellationToken stop)
    {
        lock (_gate)
        {
            _trail.Clear();
            _flow.Clear();
        }
        _runner.StopMusic();
        await Safe(() => _runner.LedStopAsync(stop));
        await Safe(() => _runner.HideAllAsync(withClock: true, stop));
        _states.Set(ShowStates.AutoBreakMusic, "aus", "PANIC");
        Bump();
    }

    async Task Quietly(Func<Task> work)
    {
        try { await work(); }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _log.LogWarning("Livestream-Titel ging nicht: {Message}", e.Message);
            NoteFault($"Livestream: {e.Message}");
        }
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
        var id = Trail(deckId).LastOrDefault()?.PageId ?? DeckPage.HomeId;
        return deck.Page(id) ?? deck.Home;
    }

    List<Step> Trail(string deckId)
    {
        lock (_gate) return _trail.TryGetValue(deckId, out var steps) ? [.. steps] : [];
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
        if (!Offered(key)) return;
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
                builder.Action(key.Number, key.Label, color, () => { Go(deckId, key.PageId, Heading(deck, key)); return Task.CompletedTask; },
                    Picture(key.Icon), symbol: Symbol(key.Icon));
                return;
            case KeyKinds.Live:
                builder.Label(key.Number, LiveText(key), color);
                return;
            default:
                return;
        }
    }

    bool Offered(DeckKey key)
    {
        var offers = _payload.Hall.Offers;
        return key.Kind switch
        {
            KeyKinds.Music => offers.Sound,
            KeyKinds.Special => SpecialKeys.ChoosesPlayer(key.Special ?? "")
                ? Shows(key) || key.Sequence.Any(step => Runs(step, offers))
                : SpecialKeys.Offered(key.Special ?? "", offers),
            KeyKinds.Steps => key.Sequence.Count == 0 || key.Sequence.Any(step => Runs(step, offers)),
            KeyKinds.Live => key.Live switch
            {
                "zeit" or "drittel" or "stand" or "stand.heim" or "stand.gast" => offers.Clock,
                "einblender" => offers.Vmix,
                _ => true,
            },
            _ => true,
        };
    }

    static bool Runs(KeyStep step, HallCapabilities offers) => step.Action switch
    {
        KeyActions.MusicPlay or KeyActions.MusicStop or KeyActions.MusicFade => offers.Sound,
        KeyActions.VmixShow or KeyActions.VmixText or KeyActions.VmixHide => offers.Vmix,
        KeyActions.LedStart or KeyActions.LedStop => offers.Led,
        _ => false,
    };

    string LiveText(DeckKey key) => key.Live switch
    {
        "zeit" => _live.Time,
        "drittel" => _live.PeriodLabel.Length > 0 ? _live.PeriodLabel : "Drittel",
        "stand" => $"{_live.HomeScore}:{_live.AwayScore}",
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
                () => ToggleStateAsync(special, stateId), Picture(key.Icon), forceText: CWhite, symbol: Symbol(key.Icon));
            return;
        }

        if (special is SpecialKeys.GuestLogo)
        {
            var guests = _payload.Game.Side("away");
            if (GuestTeams.EventOf(guests) is not { } playlist)
            {
                builder.Label(key.Number, "Logo Gast\n⚙ Spiel", CNeutral);
                return;
            }
            var name = guests.ShortName.Length > 0 ? guests.ShortName : label;
            builder.Action(key.Number, name, color, () => SpecialAsync(deckId, key, special), Picture(GuestTeams.IconOf(playlist)));
            return;
        }

        if (special is SpecialKeys.Starting6Home or SpecialKeys.Starting6Away)
        {
            var side = SpecialKeys.Side(special);
            if (Own(side) && !_payload.Game.Side(side).HasStarting6)
            {
                builder.Label(key.Number, "Starting 6 fehlt\n⚙ Spiel", CNeutral);
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

    bool Shows(DeckKey? key)
    {
        var offers = _payload.Hall.Offers;
        return key is null || (key.Livestream && offers.Vmix) || (key.Led && offers.Led);
    }

    bool OnLivestream(Flow flow) => flow.Key?.Livestream ?? true;

    bool OnLed(Flow flow) => (flow.Key?.Led ?? true) && _payload.Hall.Offers.Led;

    async Task SpecialAsync(string deckId, DeckKey key, string special)
    {
        var side = SpecialKeys.Side(special);
        if (SpecialKeys.ChoosesPlayer(special) && !Shows(key))
        {
            await _runner.RunAsync(key.Sequence, new KeyContext(side), CancellationToken.None);
            Bump();
            return;
        }
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
                if (Own(side)) Open(deckId, new Flow(FlowKind.Starting6, side, "starting6", $"Starting 6 {Team(side)}", key));
                else await _runner.ShowGuestLogoOnLedAsync(new KeyContext(side), CancellationToken.None);
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
            case SpecialKeys.GuestLogo:
                await _runner.RunAsync(key.Sequence, new KeyContext(side), CancellationToken.None);
                await _runner.ShowGuestLogoOnLedAsync(new KeyContext(side), CancellationToken.None);
                break;
            case SpecialKeys.Spectators:
                Open(deckId, new Flow(FlowKind.Spectators, "", "", "Zuschauer", key));
                break;
        }
        Bump();
    }

    string Team(string side) =>
        _payload.Game.Side(side).ShortName is { Length: > 0 } name ? name : side == "away" ? "Gast" : "Heim";

    bool Own(string side) => side.Equals(_payload.Game.OwnSide(_payload.Hall.TeamId), StringComparison.OrdinalIgnoreCase);

    void Players(Builder builder, DeckLayout deck, string deckId, Flow flow)
    {
        var team = _payload.Game.Side(flow.Side);
        var extras = new List<(string, Func<Task>)>();
        if (flow.Event == "goal") extras.Add(("Eigentor", () => PickAsync(deckId, flow, null, own: true)));
        else if (team.TopScorerPlayer is { } top) extras.Add(($"Topscorer\n{Short(top.Name)}", () => PickAsync(deckId, flow, top)));
        var prompt = flow.Event switch
        {
            "goal" => "Torschützin?",
            "penalty" => $"Strafe {PenaltyLabel(flow.Penalty)}\nwer?",
            _ => flow.Title,
        };
        Picker(builder, deck, deckId, flow, prompt, extras, player => PickAsync(deckId, flow, player));
    }

    void Assists(Builder builder, DeckLayout deck, string deckId, Flow flow) =>
        Picker(builder, deck, deckId, flow, "Assist?", [("Kein Assist", () => ToSpeakerAsync(deckId, flow, null))],
            player => ToSpeakerAsync(deckId, flow, player), CAssist, flow.Scorer);

    void Picker(Builder builder, DeckLayout deck, string deckId, Flow flow, string prompt,
        IReadOnlyList<(string Label, Func<Task> Press)> extras, Func<Player, Task> pick, int? tint = null, Player? taken = null)
    {
        var team = _payload.Game.Side(flow.Side);
        var color = tint ?? (flow.Side == "away" ? CAway : CHome);
        var places = Places(deck).ToList();
        if (places.Count < 2) return;
        var head = places[0];
        var rest = places.Skip(1).ToList();
        var keypad = team.Players.Count + extras.Count > rest.Count && rest.Count >= Keypad.Length + 1 + extras.Count;
        if (!keypad)
        {
            builder.Label(head, prompt, tint ?? CHead);
            var index = 0;
            foreach (var player in team.Players)
            {
                if (index >= rest.Count - extras.Count) break;
                var chosen = player;
                var label = $"{chosen.Display}\n{Short(chosen.Name)}";
                if (chosen.Number == taken?.Number) builder.Label(rest[index++], label, CActive);
                else builder.Action(rest[index++], label, color, () => pick(chosen));
            }
            foreach (var (label, press) in extras)
                if (index < rest.Count) builder.Action(rest[index++], label, color, press);
            return;
        }

        var found = int.TryParse(flow.Entry, out var number) ? team.Find(number) : null;
        if (flow.Entry.Length == 0) builder.Label(head, $"{prompt}\nNummer tippen", tint ?? CHead);
        else if (found is null) builder.Label(head, $"#{flow.Entry}\n?", CNeutral);
        else if (found.Number == taken?.Number) builder.Label(head, $"#{found.Display}\nschoss das Tor", CActive);
        else builder.Action(head, $"#{found.Display} ✓\n{Short(found.Name)}", CActive, () => pick(found));
        for (var i = 0; i < Keypad.Length; i++)
        {
            var digit = Keypad[i];
            builder.Action(rest[i], digit, tint ?? CNeutral, () => TypeAsync(deckId, flow, digit));
        }
        builder.Action(rest[Keypad.Length], "⌫", CNeutral, () => TypeAsync(deckId, flow, null));
        for (var i = 0; i < extras.Count; i++)
            builder.Action(rest[Keypad.Length + 1 + i], extras[i].Label, color, extras[i].Press);
    }

    static readonly string[] Keypad = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"];

    Task TypeAsync(string deckId, Flow flow, string? digit)
    {
        var entry = digit is null
            ? flow.Entry.Length > 0 ? flow.Entry[..^1] : ""
            : flow.Entry.Length >= 2 ? digit : flow.Entry + digit;
        Open(deckId, flow with { Entry = entry });
        return Task.CompletedTask;
    }

    static string PenaltyLabel(string penalty) => penalty switch
    {
        "2" => "2'",
        "22" => "2'+2'",
        "10" => "10'",
        "match" => "Spieldauer",
        _ => "",
    };

    Task ToSpeakerAsync(string deckId, Flow flow, Player? assist)
    {
        Open(deckId, flow with { Kind = FlowKind.Speaker, Assist = assist, Position = 0, Entry = "" });
        return Task.CompletedTask;
    }

    void Speaker(Builder builder, DeckLayout deck, string deckId, Flow flow)
    {
        var places = Places(deck).ToList();
        if (places.Count < 2) return;
        builder.Label(places[0], "Drücken, wenn der\nSpeaker den Namen sagt", CHead);
        if (flow.Position == 0 && flow.Scorer is { } scorer)
            builder.Action(places[1], $"LED Torschützin\n#{scorer.Display} {Short(scorer.Name)}", CBack,
                () => SpeakerLedAsync(deckId, flow, scorer, assist: false), forceText: CWhite);
        else if (flow.Position == 1 && flow.Assist is { } assist)
            builder.Action(places[1], $"LED Assist\n#{assist.Display} {Short(assist.Name)}", CBack,
                () => SpeakerLedAsync(deckId, flow, assist, assist: true), forceText: CWhite);
        else
            builder.Action(places[1], "✓ Fertig", CBack, () => { Home(deckId); return Task.CompletedTask; }, forceText: CWhite);

        if (places.Count > 2 && flow.Scorer is { } goal)
            builder.Action(places[2], $"{goal.Display}\n{Short(goal.Name)}", flow.Position == 0 ? CActive : CNeutral,
                () => SpeakerLedAsync(deckId, flow, goal, assist: false));
        if (places.Count > 3 && flow.Assist is { } helper)
            builder.Action(places[3], $"{helper.Display}\n{Short(helper.Name)}", flow.Position == 1 ? CAssist : CNeutral,
                () => SpeakerLedAsync(deckId, flow, helper, assist: true));
    }

    async Task SpeakerLedAsync(string deckId, Flow flow, Player player, bool assist)
    {
        Open(deckId, flow with { Position = !assist && flow.Assist is not null ? 1 : 2 });
        var ctx = new KeyContext(flow.Side, player, Event: assist ? "assist" : "goal");
        if (assist) await _runner.ShowAssistOnLedAsync(ctx, CancellationToken.None);
        else await _runner.ShowPlayerOnLedAsync(ctx, CancellationToken.None);
    }

    void ForgetLater(string deckId, DateTimeOffset opened) =>
        _ = Task.Delay(SpeakerTimeout).ContinueWith(_ =>
        {
            bool stale;
            lock (_gate) stale = _flow.TryGetValue(deckId, out var flow) && flow.Opened == opened && flow.Kind is FlowKind.Assist or FlowKind.Speaker;
            if (stale) Home(deckId);
        }, TaskScheduler.Default);

    void Penalty(Builder builder, DeckLayout deck, string deckId, Flow flow)
    {
        var color = flow.Side == "away" ? CAway : CHome;
        var places = Places(deck).ToList();
        var options = new[]
        {
            ("2'", "2", DeckIcons.Strafe2),
            ("2'+2'", "22", DeckIcons.Strafe22),
            ("10'", "10", DeckIcons.Strafe10),
            ("Spieldauer", "match", DeckIcons.Spieldauerstrafe),
        };
        for (var i = 0; i < options.Length && i < places.Count; i++)
        {
            var (label, value, icon) = options[i];
            builder.Action(places[i], label, color, () =>
            {
                Open(deckId, flow with { Kind = FlowKind.Players, Penalty = value });
                return Task.CompletedTask;
            }, Picture(icon));
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
            var position = i + 1;
            var shown = flow.Position == position;
            builder.Action(places[i], $"{player.Display}\n{Short(player.Name)}", shown ? CActive : color,
                () => ShowSixAsync(deckId, flow, position));
        }
        if (places.Count > 0)
            builder.Action(places[^1], flow.Position >= six.Count ? "✓ Fertig" : "▶▶ Weiter", CBack, () => ShowSixAsync(deckId, flow, flow.Position + 1));
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
                Home(deckId);
                await Quietly(() => _runner.ShowTitleAsync("spectators", new KeyContext(Text: _payload.Game.Spectators.ToString()), CancellationToken.None));
            });
    }

    async Task PickAsync(string deckId, Flow flow, Player? player, bool own = false)
    {
        var ctx = new KeyContext(flow.Side, player, flow.Penalty, Event: own ? "owngoal" : flow.Event);
        if (flow.Event == "goal") ctx = ctx with { Score = _live.GoalScore(flow.Side, GoalWindow) };
        var kind = flow.Event switch
        {
            "goal" => "player_event",
            "penalty" => "penalty",
            "bestplayer" => "bestplayer",
            _ => "name",
        };
        if (flow.Event == "goal" && !own && player is not null && Own(flow.Side) && OnLed(flow))
        {
            var opened = DateTimeOffset.UtcNow;
            Open(deckId, flow with { Kind = FlowKind.Assist, Scorer = player, Entry = "", Opened = opened });
            ForgetLater(deckId, opened);
            if (OnLivestream(flow)) await Quietly(() => _runner.ShowTitleAsync(kind, ctx, CancellationToken.None));
            return;
        }
        Home(deckId);
        if (OnLivestream(flow)) await Quietly(() => _runner.ShowTitleAsync(kind, ctx, CancellationToken.None));
        if (flow.Event != "goal" && OnLed(flow)) await _runner.ShowPlayerOnLedAsync(ctx, CancellationToken.None);
    }

    async Task ShowSixAsync(string deckId, Flow flow, int position)
    {
        var team = _payload.Game.Side(flow.Side);
        var six = team.Starting6Players;
        if (position > six.Count)
        {
            Home(deckId);
            if (OnLivestream(flow)) await Quietly(() => _runner.HideTitleAsync("starting6", CancellationToken.None));
            return;
        }
        Open(deckId, flow with { Position = position });
        var ctx = new KeyContext(flow.Side, six[position - 1], Event: position.ToString());
        if (OnLivestream(flow)) await Quietly(() => _runner.ShowTitleAsync("starting6", ctx, CancellationToken.None));
        if (OnLed(flow)) await _runner.ShowPlayerOnLedAsync(ctx, CancellationToken.None);
    }

    void Fixed(Builder builder, DeckLayout deck, string deckId, Flow flow)
    {
        var page = CurrentPage(deckId);
        var inFlow = flow.Kind != FlowKind.None;
        var deep = inFlow || (page is not null && !page.IsHome);
        var back = inFlow ? FlowBack(deck) : deck.Back;
        if (deep)
            builder.Action(back, "◀ Zurück", CBack, () => { Back(deckId); return Task.CompletedTask; },
                forceText: CWhite, top: Title(deckId));
        else if (page is null || page.KeyAt(deck.Back).IsEmpty) builder.Empty(deck.Back);
        builder.Action(deck.Panic, "🚨 PANIC", CPanic, () => PanicAsync(CancellationToken.None), forceText: CWhite);
    }

    static int FlowBack(DeckLayout deck) => deck.Panic == 1 ? deck.Back : 1;

    IEnumerable<int> Places(DeckLayout deck)
    {
        var back = FlowBack(deck);
        for (var number = 1; number <= deck.Count; number++)
            if (number != back && number != deck.Panic) yield return number;
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

    void Go(string deckId, string? pageId, string title = "")
    {
        lock (_gate)
        {
            _flow.Remove(deckId);
            if (pageId is not { Length: > 0 } || pageId == DeckPage.HomeId) _trail.Remove(deckId);
            else
            {
                if (!_trail.TryGetValue(deckId, out var steps)) _trail[deckId] = steps = [];
                steps.Add(new Step(pageId, title));
            }
        }
        Bump();
    }

    void Home(string deckId) => Go(deckId, DeckPage.HomeId);

    void Back(string deckId)
    {
        lock (_gate)
        {
            if (_flow.Remove(deckId)) { }
            else if (_trail.TryGetValue(deckId, out var steps) && steps.Count > 0) steps.RemoveAt(steps.Count - 1);
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

    static string Heading(DeckLayout deck, DeckKey key) =>
        key.Label.Length > 0 ? key.Label : deck.Page(key.PageId)?.Name ?? "";

    sealed record Step(string PageId, string Title);

    enum FlowKind { None, Players, Penalty, Starting6, Spectators, Assist, Speaker }

    sealed record Flow(FlowKind Kind, string Side, string Event, string Title, DeckKey? Key, string Penalty = "", int Position = 0,
        string Entry = "", Player? Scorer = null, Player? Assist = null, DateTimeOffset Opened = default)
    {
        public static readonly Flow None = new(FlowKind.None, "", "", "", null);
    }

    sealed class Builder(int columns, int rows)
    {
        readonly Dictionary<int, DeckSlot> _slots = [];

        int Count => columns * rows;

        public void Action(int number, string label, int bg, Func<Task> press, string? icon = null, bool active = false, string? musicId = null, int? forceText = null, string symbol = "", string subtitle = "", string top = "")
        {
            if (number < 1 || number > Count) return;
            var fg = forceText ?? Fg(bg);
            var image = top.Length > 0 ? DeckIcons.Key("", label, fg, top) : icon is null ? null : DeckIcons.Key(icon, label, fg);
            _slots[number] = new DeckSlot(number, label, image, bg, fg, true, press, active, musicId, symbol, subtitle, top);
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
