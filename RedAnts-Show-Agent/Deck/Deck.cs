using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Scenes;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Deck;

public enum DeckContext
{
    Main,
    PlayerHome, PlayerAway,
    GoalHome, GoalAway,
    PenaltyHome, PenaltyAway,
    Message,
    ScoreHome, ScoreAway,
}

public sealed record DeckSlot(int Number, string Label, string? ImageKey, int Color, int TextColor, bool Pressable, Func<Task>? Press, string? SceneId);

public sealed class Deck
{
    public const int Columns = 8;
    public const int Rows = 4;
    public const int SlotCount = Columns * Rows;

    const int CHeimBg = 0xFF7C80;
    const int CGastBg = 0xA6C9EC;
    const int CPanic = 0xFF0000;
    const int CBack = 0x8ED973;
    const int CLetter = 0x2A2A2A;
    const int CPeriod = 0xD86DCD;
    const int CCntP = 0xC1F0C8;
    const int CCntM = 0xFFCCCC;
    const int CMeld = 0xC1F0C8;
    const int CHighlit = 0x00E5FF;
    const int CRes = 0xF1A983;
    const int CNeutral = 0x555555;
    const int CInfo = 0xD9D9D9;
    const int CActive = 0xFFD400;
    const int COpener = 0xD86DCD;
    const int CProb = 0xFF6600;
    const int CWhite = 0xFFFFFF;
    const int CDark = 0x1A1A1A;
    const int CStateOff = 0x1E7B34;
    const int CStateOn = 0xC0201C;
    const int CEmpty = 0x000000;

    const string ClockOffLabel = "Uhr\n(keine Daten)";

    const int BackRow = 2, BackCol = 0;
    const int PanicRow = 3, PanicCol = 0;
    const int HeadRow = 3, HeadCol = 1;
    const int OwnGoalCol = 2;
    const int FreeCornerCol = 7;

    readonly PayloadStore _payload;
    readonly LiveGame _live;
    readonly ShowStates _states;
    readonly SceneRunner _runner;
    readonly VmixLink _vmix;
    readonly SednaLink _led;
    readonly ILogger<Deck> _log;
    readonly object _gate = new();
    TaskCompletionSource<bool> _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string _shown = "";

    public Deck(PayloadStore payload, LiveGame live, ShowStates states, SceneRunner runner, VmixLink vmix, SednaLink led, ILogger<Deck> log)
    {
        _payload = payload;
        _live = live;
        _states = states;
        _runner = runner;
        _vmix = vmix;
        _led = led;
        _log = log;
        payload.Changed += Bump;
        runner.Changed += Bump;
        runner.ContextRequested += SetContext;
        runner.Reported += (_, _) => Bump();
        live.Changed += OnLiveChanged;
        states.Changed += Bump;
    }

    public event Action<SoundCommand>? SoundRequested;

    public long Version { get; private set; } = 1;

    public DeckContext Context { get; private set; } = DeckContext.Main;

    public void SetContext(string name)
    {
        if (!Enum.TryParse<DeckContext>(name, true, out var context))
        {
            _log.LogWarning("Unbekannter Deck-Kontext {Context}", name);
            return;
        }
        lock (_gate) Context = context;
        Bump();
    }

    void OnLiveChanged()
    {
        var shown = $"{_live.HomeScore}|{_live.AwayScore}|{_live.PeriodLabel}|{_live.ClockLive}";
        lock (_gate)
        {
            if (shown == _shown) return;
            _shown = shown;
        }
        Bump();
    }

    public void Bump()
    {
        TaskCompletionSource<bool> waiters;
        lock (_gate)
        {
            Version++;
            waiters = _changed;
            _changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        waiters.TrySetResult(true);
    }

    public async Task WaitAsync(long since, TimeSpan timeout, CancellationToken stop)
    {
        Task wait;
        lock (_gate)
        {
            if (Version > since) return;
            wait = _changed.Task;
        }
        await Task.WhenAny(wait, Task.Delay(timeout, stop));
    }

    public IReadOnlyList<DeckSlot> Slots(string deckId) => BuildDeck(deckId);

    public object View() => View(DeckLayout.FieldDeck);

    public object View(string deckId)
    {
        var context = Context;
        var field = deckId == DeckLayout.FieldDeck;
        return new
        {
            version = Version,
            context = field ? context.ToString().ToLowerInvariant() : deckId,
            title = field ? Title(context) : Layout(deckId)?.Name ?? deckId,
            slots = BuildDeck(deckId).Select(s => new
            {
                number = s.Number,
                label = s.Label,
                image = s.ImageKey,
                color = s.TextColor,
                bgcolor = s.Color,
                pressable = s.Pressable,
            }).ToArray(),
        };
    }

    public Task<string?> PressAsync(int number) => PressAsync(DeckLayout.FieldDeck, number);

    public async Task<string?> PressAsync(string deckId, int number)
    {
        var slots = BuildDeck(deckId);
        if (number < 1 || number > slots.Count) return $"Taste {number} gibt es nicht";
        var slot = slots.FirstOrDefault(s => s.Number == number);
        if (slot is null || !slot.Pressable || slot.Press is null) return null;
        _log.LogInformation("Deck {Deck}/{Number}: {Label}", deckId == DeckLayout.FieldDeck ? Context.ToString() : deckId, number, slot.Label.Replace('\n', ' '));
        try
        {
            await slot.Press();
            return null;
        }
        catch (Exception e)
        {
            _log.LogWarning("Taste {Number}: {Message}", number, e.Message);
            return e.Message;
        }
    }

    string Title(DeckContext c) => c switch
    {
        DeckContext.Main => "Startseite",
        DeckContext.PlayerHome => "Spielerwahl Heim",
        DeckContext.PlayerAway => "Spielerwahl Gast",
        DeckContext.GoalHome => "Tor Heim",
        DeckContext.GoalAway => "Tor Gast",
        DeckContext.PenaltyHome => "Strafe Heim",
        DeckContext.PenaltyAway => "Strafe Gast",
        DeckContext.Message => "Mitteilung",
        DeckContext.ScoreHome => "Stand Heim",
        DeckContext.ScoreAway => "Stand Gast",
        _ => "",
    };

    public IReadOnlyList<string> Decks
    {
        get
        {
            var extra = (_payload.Hall.Decks ?? [])
                .Select(d => d.Id)
                .Where(id => id != DeckLayout.FieldDeck);
            return new[] { DeckLayout.FieldDeck }.Concat(extra).ToList();
        }
    }

    public DeckLayout? Layout(string deckId) => _payload.Hall.Deck(deckId);

    List<DeckSlot> BuildDeck(string deckId)
    {
        if (deckId == DeckLayout.FieldDeck || Layout(deckId) is not { Keys.Count: > 0 } layout) return Build(Context);
        var b = new Builder(layout.Columns, layout.Rows);
        Configured(b, layout, pages: false);
        return b.ToSlots();
    }

    List<DeckSlot> Build(DeckContext context)
    {
        var b = new Builder();
        switch (context)
        {
            case DeckContext.Main: Main(b); break;
            case DeckContext.PlayerHome: Players(b, "home", CHeimBg, goal: false); break;
            case DeckContext.PlayerAway: Players(b, "away", CGastBg, goal: false); break;
            case DeckContext.GoalHome: Players(b, "home", CHeimBg, goal: true); break;
            case DeckContext.GoalAway: Players(b, "away", CGastBg, goal: true); break;
            case DeckContext.PenaltyHome: Penalty(b, "home", CHeimBg); break;
            case DeckContext.PenaltyAway: Penalty(b, "away", CGastBg); break;
            case DeckContext.Message: Message(b); break;
            case DeckContext.ScoreHome: Score(b, "home", CHeimBg); break;
            case DeckContext.ScoreAway: Score(b, "away", CGastBg); break;
        }
        return b.ToSlots();
    }

    void Main(Builder b)
    {
        if (_payload.Hall.Deck(DeckLayout.FieldDeck) is { Keys.Count: > 0 } layout)
        {
            Configured(b, layout);
            return;
        }
        BuiltIn(b);
    }

    void Configured(Builder b, DeckLayout layout, bool pages = true)
    {
        for (var number = 1; number <= layout.Count; number++)
        {
            var row = (number - 1) / layout.Columns;
            var col = (number - 1) % layout.Columns;
            var key = layout.KeyAt(number);
            if (key is null || key.Kind == DeckKeyKinds.Empty || !Offered(key)) { b.Empty(row, col); continue; }
            KeyOnMain(b, row, col, key);
        }
        if (pages)
        {
            if (layout.KeyAt(BackRow * Columns + BackCol + 1) is null) Back(b);
            if (layout.KeyAt(PanicRow * Columns + PanicCol + 1) is null) Panic(b);
        }
        b.Highlight(_runner.Running, CActive);
    }

    bool Offered(DeckKey key)
    {
        var offers = _payload.Hall.Offers;
        return key.Kind switch
        {
            DeckKeyKinds.Sound => offers.Sound,
            DeckKeyKinds.State => StateOffered(key.StateId ?? ""),
            DeckKeyKinds.Live => key.Live != "clock" || offers.Clock,
            _ => true,
        };
    }

    bool StateOffered(string stateId)
    {
        var offers = _payload.Hall.Offers;
        return stateId switch
        {
            ShowStates.Music => offers.Sound,
            ShowStates.Led or ShowStates.Sponsors => offers.Led,
            ShowStates.Light => offers.Light,
            ShowStates.Overlay => offers.Vmix,
            ShowStates.ClockVisible or ShowStates.ClockRunning => offers.Clock && offers.Vmix,
            _ => true,
        };
    }

    void KeyOnMain(Builder b, int row, int col, DeckKey key)
    {
        var color = Rgb(key.Color);
        switch (key.Kind)
        {
            case DeckKeyKinds.Back:
                Back(b);
                break;
            case DeckKeyKinds.Scene:
                if (_payload.Scene(key.SceneId ?? "") is null) b.Label(row, col, key.Label, CNeutral);
                else b.Action(row, col, key.Label, color, () => Run(key.SceneId!), key.Icon, key.SceneId);
                break;
            case DeckKeyKinds.Sound:
                if (_payload.Sound(key.SoundId) is not { } sound) b.Label(row, col, key.Label, CNeutral);
                else b.Action(row, col, key.Label.Length > 0 ? key.Label : sound.Label, color, () => PlaySound(sound), key.Icon);
                break;
            case DeckKeyKinds.Page:
                b.Nav(row, col, key.Label, color, () => SetContext(key.Page ?? nameof(DeckContext.Main)), key.Icon);
                break;
            case DeckKeyKinds.Live:
                LiveKey(b, row, col, key, color);
                break;
            case DeckKeyKinds.State:
                StateKey(b, row, col, key);
                break;
            default:
                b.Label(row, col, key.Label, color);
                break;
        }
    }

    void LiveKey(Builder b, int row, int col, DeckKey key, int color)
    {
        switch (key.Live)
        {
            case "score.home":
                b.Nav(row, col, _live.HomeScore, color, () => SetContext(nameof(DeckContext.ScoreHome)));
                break;
            case "score.away":
                b.Nav(row, col, _live.AwayScore, color, () => SetContext(nameof(DeckContext.ScoreAway)));
                break;
            case "clock":
                b.Label(row, col, _live.ClockLive ? _live.Time : ClockOffLabel, _live.ClockLive ? CStateOff : CNeutral, forceText: CWhite);
                break;
            default:
                b.Label(row, col, _live.PeriodLabel.Length > 0 ? _live.PeriodLabel : key.Label, color);
                break;
        }
    }

    void StateKey(Builder b, int row, int col, DeckKey key)
    {
        var value = _states.Value(key.StateId ?? "");
        var rows = key.Rows ?? [];
        var match = rows.FirstOrDefault(r => r.Value.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? rows.FirstOrDefault();
        if (match is null) { b.Label(row, col, key.Label, CNeutral); return; }
        var color = Rgb(match.Color);
        if (match.Behaviour != DeckStateRow.RunScene || match.SceneId is not { Length: > 0 } scene)
        {
            b.Label(row, col, match.Label, match.Behaviour == DeckStateRow.Locked ? CNeutral : color, forceText: CWhite);
            return;
        }
        b.Action(row, col, match.Label, color, () => Run(scene), match.Icon, scene, forceText: CWhite);
    }

    Task PlaySound(SoundCommand sound)
    {
        _log.LogInformation("Musik {Label} angefordert", sound.Label);
        SoundRequested?.Invoke(sound);
        return Task.CompletedTask;
    }

    static int Rgb(string? color)
    {
        if (color is not { Length: 7 } hex || hex[0] != '#') return CNeutral;
        return int.TryParse(hex[1..], System.Globalization.NumberStyles.HexNumber, null, out var value) ? value : CNeutral;
    }

    void BuiltIn(Builder b)
    {
        Scene(b, 0, 0, "Tor", CHeimBg, "tor-heim", DeckIcons.Tor);
        Scene(b, 0, 1, "Coach", CHeimBg, "coach-heim", DeckIcons.Coach);
        Scene(b, 0, 2, "Timeout", CHeimBg, "timeout-heim", DeckIcons.Timeout);
        Scene(b, 0, 3, "Aufstellung", CHeimBg, "aufstellung-heim", DeckIcons.Aufstellung);
        Scene(b, 0, 4, "Aufstellung", CGastBg, "aufstellung-gast", DeckIcons.Aufstellung);
        Scene(b, 0, 5, "Timeout", CGastBg, "timeout-gast", DeckIcons.Timeout);
        Scene(b, 0, 6, "Coach", CGastBg, "coach-gast", DeckIcons.Coach);
        Scene(b, 0, 7, "Tor", CGastBg, "tor-gast", DeckIcons.Tor);

        b.Nav(1, 0, "Strafe", CHeimBg, () => SetContext(nameof(DeckContext.PenaltyHome)), DeckIcons.Strafe);
        Scene(b, 1, 1, "Spieler", CHeimBg, "name-heim", DeckIcons.Spieler);
        Scene(b, 1, 2, "Best Player", CHeimBg, "bestplayer-heim", DeckIcons.BestPlayer);
        Starting6Key(b, 1, 3, "home", CHeimBg);
        Starting6Key(b, 1, 4, "away", CGastBg);
        Scene(b, 1, 5, "Best Player", CGastBg, "bestplayer-gast", DeckIcons.BestPlayer);
        Scene(b, 1, 6, "Spieler", CGastBg, "name-gast", DeckIcons.Spieler);
        b.Nav(1, 7, "Strafe", CGastBg, () => SetContext(nameof(DeckContext.PenaltyAway)), DeckIcons.Strafe);

        b.Nav(2, 1, "Mitteilung", CMeld, () => SetContext(nameof(DeckContext.Message)), DeckIcons.Meldung);
        b.Label(2, 2, _live.PeriodLabel.Length > 0 ? _live.PeriodLabel : "Drittel", CPeriod);
        b.Nav(2, 3, _live.HomeScore, CHeimBg, () => SetContext(nameof(DeckContext.ScoreHome)));
        b.Nav(2, 4, _live.AwayScore, CGastBg, () => SetContext(nameof(DeckContext.ScoreAway)));
        Scene(b, 2, 5, "Highlight", CHighlit, "highlight", DeckIcons.Highlight);
        Scene(b, 2, 6, "Resultat", CRes, "resultat", DeckIcons.Resultat);
        Scene(b, 2, 7, "Opener", COpener, "opener", DeckIcons.Opener);

        Panic(b);
        Scene(b, 3, 1, "Kommentar", CInfo, "kommentatoren", DeckIcons.Kommentar);
        Scene(b, 3, 2, "Schiri", CInfo, "schiedsrichter", DeckIcons.Schiedsrichter);
        b.Label(3, 3, _live.ClockLive ? _live.Time : "Uhr\n(keine Daten)", _live.ClockLive ? CStateOff : CNeutral, forceText: CWhite);

        var sponsors = _led.LastCommand?.Contains("Sponsoren", StringComparison.OrdinalIgnoreCase) ?? false;
        Scene(b, 3, 6, "Werbung", sponsors ? CStateOn : CStateOff, "sponsoren", DeckIcons.Werbung, forceText: CWhite);
        Scene(b, 3, 7, "Einblender", _vmix.LiveTitle is not null ? CStateOn : CStateOff, "ausblenden", DeckIcons.EinblenderEin, forceText: CWhite);

        b.Highlight(_runner.Running, CActive);
    }

    void Scene(Builder b, int row, int col, string label, int bg, string sceneId, string? icon = null, int? forceText = null)
    {
        if (_payload.Scene(sceneId) is null)
        {
            b.Label(row, col, label, CNeutral);
            return;
        }
        b.Action(row, col, label, bg, () => Run(sceneId), icon, sceneId, forceText);
    }

    Task Run(string sceneId, Dictionary<string, string>? args = null)
    {
        _ = _runner.Start(sceneId, args ?? new Dictionary<string, string>());
        return Task.CompletedTask;
    }

    void Starting6Key(Builder b, int row, int col, string side, int bg)
    {
        var team = _payload.Game.Side(side);
        var sceneId = side == "away" ? "starting6-gast" : "starting6-heim";
        if (!team.HasStarting6 || _payload.Scene(sceneId) is null)
        {
            b.Label(row, col, "Starting 6\n(Aufstellung\nunvollständig)", CNeutral);
            return;
        }
        b.Action(row, col, "Starting 6", bg, () => Run(sceneId), DeckIcons.Starting6, sceneId);
    }

    void Players(Builder b, string side, int bg, bool goal)
    {
        var players = _payload.Game.Side(side).Players;
        var wanted = _runner.WantsPick && _runner.PickSide.Equals(side, StringComparison.OrdinalIgnoreCase);

        var i = 0;
        foreach (var (row, col) in PlayerPositions(goal))
        {
            var player = i < players.Count ? players[i] : null;
            if (player is null) b.Empty(row, col);
            else if (!wanted) b.Label(row, col, $"{player.Display}\n{Trunc(player.Name, 10)}", bg);
            else b.Action(row, col, $"{player.Display}\n{Trunc(player.Name, 10)}", bg, () => PickAsync(player, ownGoal: false));
            i++;
        }
        if (players.Count > i)
            _log.LogWarning("Kader {Side}: {Count} Spielerinnen, aber nur {Slots} Plätze auf dem Deck", side, players.Count, i);

        Back(b);
        Panic(b);
        b.Label(HeadRow, HeadCol, _runner.RunningLabel.Length > 0 ? _runner.RunningLabel : (goal ? "Tor" : "Spielerin"), bg);
        if (goal && wanted && players.Count > 0)
            b.Action(3, OwnGoalCol, "Eigentor", bg, () => PickAsync(players[0], ownGoal: true), DeckIcons.Tor);
    }

    Task PickAsync(Player player, bool ownGoal)
    {
        if (!_runner.Pick(player, ownGoal)) return Task.CompletedTask;
        SetContext(nameof(DeckContext.Main));
        return Task.CompletedTask;
    }

    static IEnumerable<(int Row, int Col)> PlayerPositions(bool goal)
    {
        for (var row = 0; row < Rows; row++)
            for (var col = 0; col < Columns; col++)
            {
                if (row == BackRow && col == BackCol) continue;
                if (row == 3 && (col == PanicCol || col == HeadCol)) continue;
                if (row == 3 && col == FreeCornerCol) continue;
                if (row == 3 && col == OwnGoalCol && goal) continue;
                yield return (row, col);
            }
    }

    void Penalty(Builder b, string side, int bg)
    {
        var sceneId = side == "away" ? "strafe-gast" : "strafe-heim";
        if (_payload.Scene(sceneId) is null) b.Label(0, 0, "Strafe\n(keine Szene)", CNeutral);
        else
        {
            PenaltyKey(b, 0, 0, "2'", "2", bg, sceneId);
            PenaltyKey(b, 0, 1, "2'+2'", "2+2", bg, sceneId);
            PenaltyKey(b, 0, 2, "10'", "10", bg, sceneId);
            PenaltyKey(b, 0, 3, "Match", "Matchstrafe", bg, sceneId);
        }
        Back(b);
        Panic(b);
        Letters(b, "STRAFE");
    }

    void PenaltyKey(Builder b, int row, int col, string label, string penalty, int bg, string sceneId) =>
        b.Action(row, col, label, bg, () => Run(sceneId, new Dictionary<string, string> { ["penalty"] = penalty }), DeckIcons.Strafe);

    void Message(Builder b)
    {
        var shortcuts = _payload.Hall.Shortcuts;
        var hasScene = _payload.Scene("mitteilung") is not null;
        var i = 0;
        foreach (var (row, col) in MessagePositions())
        {
            if (i >= shortcuts.Count) break;
            var text = shortcuts[i++];
            if (hasScene)
                b.Action(row, col, Wrap(text), i == 1 ? CProb : CInfo,
                    () => Run("mitteilung", new Dictionary<string, string> { ["text"] = text }), i == 1 ? DeckIcons.Meldung : null);
            else b.Label(row, col, Wrap(text), CNeutral);
        }
        Back(b);
        Panic(b);
        Letters(b, "MELDUNG");
    }

    static IEnumerable<(int Row, int Col)> MessagePositions()
    {
        for (var row = 0; row < 3; row++)
            for (var col = 0; col < Columns; col++)
                if (!(row == BackRow && col == BackCol)) yield return (row, col);
    }

    void Score(Builder b, string side, int bg)
    {
        var home = side == "home";
        var team = _payload.Game.Side(side);
        b.Label(0, 3, team.LongName.Length > 0 ? team.LongName : (home ? "Heim" : "Gast"), bg);

        if (_live.ClockLive)
        {
            b.Label(1, 1, "−1\n(Uhr liefert\nden Stand)", bg);
            b.Label(1, 5, "+1\n(Uhr liefert\nden Stand)", bg);
        }
        else
        {
            b.Action(1, 1, "−1", CCntM, () => Adjust(side, -1));
            b.Action(1, 5, "+1", CCntP, () => Adjust(side, +1));
        }
        b.Label(1, 3, home ? _live.HomeScore : _live.AwayScore, bg);

        Back(b);
        Panic(b);
        Letters(b, "STAND", bg);
        b.Fill(bg);
    }

    Task Adjust(string side, int delta)
    {
        _live.AdjustScore(side, delta);
        return Task.CompletedTask;
    }

    void Back(Builder b) => b.Nav(BackRow, BackCol, "◀ Zurück", CBack, () =>
    {
        if (_runner.WantsPick) _runner.Abort();
        SetContext(nameof(DeckContext.Main));
    });

    void Panic(Builder b) =>
        b.Action(PanicRow, PanicCol, "🚨 PANIC", CPanic, () => Run(SceneRunner.PanicScene), null, null, CWhite);

    static void Letters(Builder b, string word, int color = CLetter)
    {
        for (var i = 0; i < word.Length && i < 6; i++)
        {
            var text = i < 5 ? word[i].ToString() : word[5..];
            b.Label(3, i + 1, text, color, forceText: color == CLetter ? CWhite : null);
        }
    }

    static string Trunc(string s, int max) => s.Length <= max ? s : s[..max];

    static string Wrap(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var line = "";
        foreach (var word in words)
        {
            if (line.Length > 0 && line.Length + word.Length + 1 > 12)
            {
                lines.Add(line);
                line = "";
                if (lines.Count == 3) break;
            }
            line = line.Length == 0 ? word : line + " " + word;
        }
        if (lines.Count < 3 && line.Length > 0) lines.Add(line);
        return string.Join('\n', lines.Select(l => Trunc(l, 14)));
    }

    sealed class Builder(int columns = Columns, int rows = Rows)
    {
        readonly Dictionary<int, DeckSlot> _slots = [];

        int Count => columns * rows;

        int Number(int row, int col) => row * columns + col + 1;

        public void Action(int row, int col, string label, int bg, Func<Task> press, string? icon = null, string? sceneId = null, int? forceText = null)
        {
            var fg = forceText ?? Fg(bg);
            _slots[Number(row, col)] = new DeckSlot(Number(row, col), label, icon is null ? null : DeckIcons.Key(icon, label, fg), bg, fg, true, press, sceneId);
        }

        public void Nav(int row, int col, string label, int bg, Action navigate, string? icon = null) =>
            Action(row, col, label, bg, () => { navigate(); return Task.CompletedTask; }, icon);

        public void Label(int row, int col, string label, int bg, int? forceText = null)
        {
            var fg = forceText ?? Fg(bg);
            _slots[Number(row, col)] = new DeckSlot(Number(row, col), label, null, bg, fg, false, null, null);
        }

        public void Empty(int row, int col) =>
            _slots[Number(row, col)] = new DeckSlot(Number(row, col), "", null, CEmpty, CWhite, false, null, null);

        public void Fill(int color)
        {
            for (var n = 1; n <= Count; n++)
                if (!_slots.ContainsKey(n)) _slots[n] = new DeckSlot(n, "", null, color, Fg(color), false, null, null);
        }

        public void Highlight(string? sceneId, int color)
        {
            if (string.IsNullOrEmpty(sceneId)) return;
            foreach (var key in _slots.Keys.ToList())
            {
                var s = _slots[key];
                if (s.SceneId != sceneId) continue;
                var fg = Fg(color);
                _slots[key] = s with { Color = color, TextColor = fg, ImageKey = s.ImageKey is null ? null : s.ImageKey[..s.ImageKey.LastIndexOf('|')] + $"|{fg:X6}" };
            }
        }

        static int Fg(int bg)
        {
            int r = (bg >> 16) & 0xFF, g = (bg >> 8) & 0xFF, bl = bg & 0xFF;
            var luma = (0.299 * r + 0.587 * g + 0.114 * bl) / 255.0;
            return luma > 0.55 ? CDark : CWhite;
        }

        public List<DeckSlot> ToSlots()
        {
            var all = new List<DeckSlot>(Count);
            for (var n = 1; n <= Count; n++)
                all.Add(_slots.TryGetValue(n, out var s) ? s : new DeckSlot(n, "", null, CEmpty, CWhite, false, null, null));
            return all;
        }
    }
}
