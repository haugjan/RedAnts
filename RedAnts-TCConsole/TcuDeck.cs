namespace TcuConsole;

/// <summary>
/// Das Deck als 32 Tasten — vier Zeilen zu acht Spalten, durchnummeriert von
/// 1 (oben links) bis 32 (unten rechts).
///
/// Das Companion-Modul kennt ausschliesslich diese Nummern. Was auf einer Taste
/// steht und was ein Druck auslöst, entscheidet allein diese Klasse: die
/// Zuordnung wird in Companion EINMAL angelegt und ändert sich nie wieder.
///
/// Damit ersetzt das Deck die frühere generierte .companionconfig mit ihren elf
/// Seiten. Die Seiten leben als <see cref="DeckContext"/> weiter, nur navigiert
/// jetzt TcuConsole selbst zwischen ihnen statt Companion.
/// </summary>
public sealed class TcuDeck(TcuUdp udp, TcuLower lower, TcuGameState game, TcuLogger logger, bool clockControl)
{
    /// <summary>Mit Zeitsteuerung: Uhr Start/Stop, −1 s und +1 s stehen auf der
    /// Startseite (Tasten 28 bis 30). Ohne bleiben diese Plätze leer — sie
    /// rücken nicht nach, weil die Zuordnung in Companion fest ist. Beim Start
    /// von TcuConsole gewählt.</summary>
    public bool ClockControl => clockControl;

    public const int Columns = 8;
    public const int Rows    = 4;
    public const int SlotCount = Columns * Rows;

    // ── Farben (RGB int), übernommen aus der früheren Config ──────────────────
    const int CHeimBg   = 0xFF7C80;
    const int CGastBg   = 0xA6C9EC;
    const int CPanic    = 0xFF0000;
    const int CBack     = 0x8ED973;
    const int CLetter   = 0x2A2A2A;
    const int CPeriod   = 0xD86DCD;
    const int CCntP     = 0xC1F0C8;
    const int CCntM     = 0xFFCCCC;
    const int CTimer    = 0xFF6600;
    const int CMeld     = 0xC1F0C8;
    const int CHighlit  = 0x00E5FF;
    const int CRes      = 0xF1A983;
    const int CNeutral  = 0x555555;
    const int CInfo     = 0xD9D9D9;
    const int CActive   = 0xFFD400;
    const int COpener   = 0xD86DCD;
    const int CProb     = 0xFF6600;
    const int CDisp     = 0xD86DCD;
    const int CWhite    = 0xFFFFFF;
    const int CDark     = 0x1A1A1A;
    const int CStateOff = 0x1E7B34;
    const int CStateOn  = 0xC0201C;
    const int CEmpty    = 0x000000;

    // Feste Plätze, auf jedem Kontext gleich — das ist der Grund, warum die
    // Zuordnung in Companion einmalig bleibt.
    const int BackRow = 2, BackCol = 0;
    const int PanicRow = 3, PanicCol = 0;
    const int HeadRow = 3, HeadCol = 1;
    const int OwnGoalCol = 2;
    const int FreeCornerCol = 7;

    readonly object _gate = new();
    TaskCompletionSource<bool> _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Zählt jede sichtbare Änderung. Das Modul fragt mit "since" und
    /// bekommt erst Antwort, wenn sich etwas bewegt hat.</summary>
    public long Version { get; private set; } = 1;

    public DeckContext Context { get; private set; } = DeckContext.Main;

    /// <summary>Der scharf gestellte Modus — die zugehörige Taste leuchtet.
    /// Gesetzt beim Druck, gelöscht von PANIC.</summary>
    string _activeMode = "";

    /// <summary>Letzter gelesener Live-Zustand. Liefert Stand, Drittel und die
    /// Farben der Zustandsknöpfe.</summary>
    public TcuState.Snapshot? Live { get; private set; }

    public void Update(TcuState.Snapshot snapshot)
    {
        lock (_gate)
        {
            // Nur melden, was das Deck auch zeigt. Die Spieluhr tickt jede
            // Sekunde weiter, steht aber nicht auf dem Deck — ohne diesen
            // Vergleich liefe der Long-Poll im Sekundentakt leer durch.
            if (Live is not null && !Differs(Live, snapshot)) { Live = snapshot; return; }
            Live = snapshot;
        }
        Bump();
    }

    // Ohne Zeitsteuerung steht keine Uhr-Taste auf dem Deck, die umfärben
    // müsste — ein Start oder Stop der Uhr darf dann keinen Long-Poll wecken.
    bool Differs(TcuState.Snapshot a, TcuState.Snapshot b) =>
        a.ScoreHome != b.ScoreHome || a.ScoreAway != b.ScoreAway ||
        a.PeriodLabel != b.PeriodLabel || (clockControl && a.ClockRunning != b.ClockRunning) ||
        a.LowerThirdLive != b.LowerThirdLive || a.SponsorAuto != b.SponsorAuto ||
        a.Match != b.Match;

    /// <summary>Weckt alle wartenden Long-Polls.</summary>
    public void Bump()
    {
        TaskCompletionSource<bool> waiters;
        lock (_gate)
        {
            Version++;
            waiters  = _changed;
            _changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        waiters.TrySetResult(true);
    }

    /// <summary>Wartet, bis <see cref="Version"/> über <paramref name="since"/>
    /// steht — höchstens aber die angegebene Zeit. Länger als der Timeout des
    /// Moduls darf das nicht dauern, sonst bricht dort die Anfrage ab.</summary>
    public async Task WaitAsync(long since, TimeSpan timeout, CancellationToken token)
    {
        Task wait;
        lock (_gate)
        {
            if (Version > since) return;
            wait = _changed.Task;
        }
        await Task.WhenAny(wait, Task.Delay(timeout, token));
    }

    // ── Ansicht ──────────────────────────────────────────────────────────────

    /// <summary>Die 32 Tasten des aktuellen Kontexts, als DTO fürs Modul.</summary>
    public object View()
    {
        var slots = Build(Context);
        return new
        {
            version = Version,
            context = Context.ToString().ToLowerInvariant(),
            title   = Title(Context),
            slots   = slots.Select(s => new
            {
                number    = s.Number,
                label     = s.Label,
                image     = s.ImageKey,
                color     = s.TextColor,
                bgcolor   = s.Color,
                pressable = s.Pressable,
            }).ToArray(),
        };
    }

    /// <summary>Das fertige Tastenbild zu einem Schlüssel aus der Ansicht.
    /// Getrennt von der Ansicht, weil 32 Base64-Bilder bei jedem Takt
    /// unverhältnismässig wären — das Modul holt jedes Bild einmal und merkt es
    /// sich.</summary>
    public static string? Image(string key)
    {
        // Schlüssel: name|label|textfarbe (hex). Genau die drei Angaben, mit
        // denen TcuIcons sein eigenes Bild aufbaut und zwischenspeichert.
        var parts = key.Split('|');
        if (parts.Length != 3) return null;
        if (!int.TryParse(parts[2], System.Globalization.NumberStyles.HexNumber, null, out var fg)) return null;
        return TcuIcons.Get(parts[0], parts[1], fg);
    }

    static string Title(DeckContext c) => c switch
    {
        DeckContext.Main         => "Startseite",
        DeckContext.PlayerHome   => "Spielerwahl Heim",
        DeckContext.PlayerAway   => "Spielerwahl Gast",
        DeckContext.GoalHome     => "Tor Heim",
        DeckContext.GoalAway     => "Tor Gast",
        DeckContext.Starting6Home => "Starting 6 Heim",
        DeckContext.Starting6Away => "Starting 6 Gast",
        DeckContext.PenaltyHome  => "Strafe Heim",
        DeckContext.PenaltyAway  => "Strafe Gast",
        DeckContext.Message      => "Meldung",
        DeckContext.Period       => "Drittel",
        _                        => "",
    };

    // ── Druck ────────────────────────────────────────────────────────────────

    /// <summary>Führt aus, was die Taste im aktuellen Kontext bedeutet.
    /// Rückgabe: Meldung fürs Log, null bei Erfolg.</summary>
    public async Task<string?> PressAsync(int number)
    {
        if (number < 1 || number > SlotCount) return $"Taste {number} gibt es nicht";

        var slot = Build(Context).FirstOrDefault(s => s.Number == number);
        if (slot is null || !slot.Pressable) return null;

        logger.LogUi($"Deck {Context}/{number}: {(slot.Label.Length > 0 ? slot.Label : slot.ImageKey)}");

        if (slot.Mode is not null) _activeMode = slot.Mode;

        string? note = null;
        foreach (var cmd in slot.Commands)
        {
            if (cmd.StartsWith("TcuUi=", StringComparison.OrdinalIgnoreCase))
            {
                var parts = cmd["TcuUi=".Length..].Split('|');
                note ??= await lower.ExecuteAsync(parts[0].Trim().ToLowerInvariant(), parts);
            }
            else
            {
                udp.Send(cmd);
                await Task.Delay(50);
            }
        }

        if (slot.Target is DeckContext target)
        {
            lock (_gate) Context = target;
        }

        Bump();
        return note;
    }

    /// <summary>Zurück auf die Startseite — nach PANIC und nach jedem Vorgang,
    /// der abgeschlossen ist.</summary>
    public void GoHome()
    {
        lock (_gate) Context = DeckContext.Main;
        Bump();
    }

    // ── Aufbau der Kontexte ──────────────────────────────────────────────────

    List<DeckSlot> Build(DeckContext context)
    {
        var b = new DeckBuilder();

        switch (context)
        {
            case DeckContext.Main:          Main(b);                                  break;
            case DeckContext.PlayerHome:    Players(b, "home", CHeimBg, goal: false); break;
            case DeckContext.PlayerAway:    Players(b, "away", CGastBg, goal: false); break;
            case DeckContext.GoalHome:      Players(b, "home", CHeimBg, goal: true);  break;
            case DeckContext.GoalAway:      Players(b, "away", CGastBg, goal: true);  break;
            case DeckContext.Starting6Home: Starting6(b, "home", CHeimBg);            break;
            case DeckContext.Starting6Away: Starting6(b, "away", CGastBg);            break;
            case DeckContext.PenaltyHome:   Penalty(b, "home", CHeimBg);              break;
            case DeckContext.PenaltyAway:   Penalty(b, "away", CGastBg);              break;
            case DeckContext.Message:       Message(b);                               break;
            case DeckContext.Period:        Period(b);                                break;
        }

        return b.ToSlots();
    }

    // ── Startseite ───────────────────────────────────────────────────────────
    void Main(DeckBuilder b)
    {
        // Zeile 0 — Tor, Coach, Timeout, Aufstellung je Mannschaft
        b.Action(0, 0, "Tor",         CHeimBg, ["TcuUi=prepare|goal|home"], TcuIcons.Tor,         target: DeckContext.GoalHome, mode: "tor_home");
        b.Action(0, 1, "Coach",       CHeimBg, ["TcuUi=lt|coach_home"],     TcuIcons.Coach,       mode: "coach_home");
        b.Action(0, 2, "Timeout",     CHeimBg, ["TcuUi=lt|timeout_home"],   TcuIcons.Timeout,     mode: "timeout_home");
        b.Action(0, 3, "Aufstellung", CHeimBg, ["TcuUi=lt|lineup_home"],    TcuIcons.Aufstellung, mode: "lineup_home");
        b.Action(0, 4, "Aufstellung", CGastBg, ["TcuUi=lt|lineup_away"],    TcuIcons.Aufstellung, mode: "lineup_away");
        b.Action(0, 5, "Timeout",     CGastBg, ["TcuUi=lt|timeout_away"],   TcuIcons.Timeout,     mode: "timeout_away");
        b.Action(0, 6, "Coach",       CGastBg, ["TcuUi=lt|coach_away"],     TcuIcons.Coach,       mode: "coach_away");
        b.Action(0, 7, "Tor",         CGastBg, ["TcuUi=prepare|goal|away"], TcuIcons.Tor,         target: DeckContext.GoalAway, mode: "tor_away");

        // Zeile 1 — Strafe, Spieler, Best Player, Starting 6
        b.Nav   (1, 0, "Strafe",      CHeimBg, DeckContext.PenaltyHome, TcuIcons.Strafe);
        b.Action(1, 1, "Spieler",     CHeimBg, ["TcuUi=prepare|name|home"], TcuIcons.Spieler,    target: DeckContext.PlayerHome, mode: "name_home");
        b.Action(1, 2, "Best Player", CHeimBg, ["TcuUi=prepare|best|home"], TcuIcons.BestPlayer, target: DeckContext.PlayerHome, mode: "best_home");
        b.Action(1, 3, "Starting 6",  CHeimBg, ["TcuUi=s6|home"],           TcuIcons.Starting6,  target: DeckContext.Starting6Home, mode: "s6_home");
        b.Action(1, 4, "Starting 6",  CGastBg, ["TcuUi=s6|away"],           TcuIcons.Starting6,  target: DeckContext.Starting6Away, mode: "s6_away");
        b.Action(1, 5, "Best Player", CGastBg, ["TcuUi=prepare|best|away"], TcuIcons.BestPlayer, target: DeckContext.PlayerAway, mode: "best_away");
        b.Action(1, 6, "Spieler",     CGastBg, ["TcuUi=prepare|name|away"], TcuIcons.Spieler,    target: DeckContext.PlayerAway, mode: "name_away");
        b.Nav   (1, 7, "Strafe",      CGastBg, DeckContext.PenaltyAway, TcuIcons.Strafe);

        // Zeile 2 — Meldung, Spielstand, Grafiksteuerung
        b.Nav  (2, 1, "Meldung", CMeld, DeckContext.Message, TcuIcons.Meldung);
        b.Nav  (2, 2, Live?.PeriodLabel ?? "Drittel", CPeriod, DeckContext.Period);
        b.Label(2, 3, Live?.ScoreHome ?? "–", CHeimBg);
        b.Label(2, 4, Live?.ScoreAway ?? "–", CGastBg);
        b.Action(2, 5, "Highlight", CHighlit, ["TcuUi=highlight"],  TcuIcons.Highlight, mode: "highlight");
        b.Action(2, 6, "Resultat",  CRes,     ["TcuUi=lt|result"],  TcuIcons.Resultat,  mode: "result");
        b.Action(2, 7, "Opener",    COpener,  ["TcuUi=lt|opener"],  TcuIcons.Opener,    mode: "opener");

        // Zeile 3 — System, Spieluhr, Zustandsknöpfe
        Panic(b, home: false);
        b.Action(3, 1, "Kommentar", CInfo, ["TcuUi=lt|commentary"], TcuIcons.Kommentar,      mode: "commentary");
        b.Action(3, 2, "Schiri",    CInfo, ["TcuUi=lt|referee"],    TcuIcons.Schiedsrichter, mode: "referee");
        // Spieluhr nur mit Zeitsteuerung. Ohne bleiben die drei Plätze leer:
        // DeckBuilder füllt fehlende Tasten mit schwarzen, nicht drückbaren auf.
        if (clockControl)
        {
            b.Action(3, 3, "◀ Uhr −1 s\n(aus)", CTimer, ["TcuController=scoreboard_hide", "TcuController=scoreboard_secmin"]);
            b.Action(3, 4, "▶ Uhr +1 s\n(aus)", CTimer, ["TcuController=scoreboard_hide", "TcuController=scoreboard_secplus"]);

            // Uhr: grün solange sie läuft, rot sobald sie steht — umgekehrt zu
            // Einblender und Werbung, wo rot "auf Sendung" heisst. Mitten im Spiel
            // ist die stehende Uhr der Zustand, den man sofort sehen will.
            b.Action(3, 5, "⏱ Uhr\nStart / Stop",
                     (Live?.ClockRunning ?? false) ? CStateOff : CStateOn,
                     ["TcuController=scoreboard_show", "TcuController=scoreboard_togglestartstop"]);
        }

        var werbung = lower.SponsorOn(Live?.SponsorAuto ?? false);
        b.Action(3, 6, "Werbung",    werbung ? CStateOn : CStateOff, ["TcuUi=sponsor|toggle"], TcuIcons.Werbung,       forceText: CWhite);
        b.Action(3, 7, "Einblender", (Live?.LowerThirdLive ?? false) ? CStateOn : CStateOff, ["TcuUi=toggle"], TcuIcons.EinblenderEin, forceText: CWhite);

        b.Highlight(_activeMode, CActive);
    }

    // ── Spielerwahl ──────────────────────────────────────────────────────────
    //
    // Der Knopf meldet seinen Platz, nicht die Nummer: welcher Spieler auf
    // Platz 7 steht, löst TcuLower aus dem aktuellen Kader auf. Ein neuer Kader
    // ändert damit nur die Beschriftung — und die kommt jetzt ohnehin live.
    void Players(DeckBuilder b, string side, int bg, bool goal)
    {
        var kader = side == "away" ? game.AwayPlayers : game.HomePlayers;

        var i = 0;
        foreach (var (row, col) in PlayerPositions(goal))
        {
            var p = i < kader.Count ? kader[i] : null;
            if (p is null) b.Empty(row, col);
            else b.Action(row, col, $"{p.Display}\n{Trunc(p.Name, 10)}", bg,
                          [$"TcuUi=player|{side}|#{i}"], target: DeckContext.Main);
            i++;
        }

        if (kader.Count > i)
            logger.Log($"Kader {side}: {kader.Count} Spieler, aber nur {i} Plätze — " +
                       $"die letzten {kader.Count - i} fehlen auf dem Deck", LogLevel.Warning);

        Back(b);
        Panic(b, home: true);

        // Kopfzeile: wofür gilt der nächste Spielerdruck? Ohne diesen Hinweis
        // wäre nach dem Kontextwechsel nicht mehr erkennbar, welcher Modus
        // scharf ist — die Seite dient allen vier Fällen.
        b.Label(HeadRow, HeadCol, lower.HeadLabel.Length > 0 ? lower.HeadLabel : (goal ? "Tor" : "Spieler"), bg);

        if (goal)
            b.Action(3, OwnGoalCol, "Eigentor", bg, [$"TcuUi=owngoal|{side}"],
                     TcuIcons.Tor, target: DeckContext.Main);
    }

    /// <summary>Alle Plätze des Spielerrasters — auch die unbelegten, damit das
    /// Raster bei kleinerem Kader nicht verrutscht.</summary>
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

    // ── Starting 6 ───────────────────────────────────────────────────────────
    //
    // Die sechs Knöpfe zeigen nur die Namen. TCunihockey führt die Reihe selbst:
    // "Weiter" blendet beim ersten Druck ein und blättert danach je einen weiter.
    void Starting6(DeckBuilder b, string side, int bg)
    {
        var six = game.Starting6(side);
        for (var i = 0; i < 6; i++)
        {
            var p = i < six.Count ? six[i] : null;
            if (p is null) b.Empty(0, i);
            else b.Label(0, i, $"{p.Display}\n{Trunc(p.Name, 10)}", bg);
        }

        b.Action(0, 7, "▶▶ Weiter", CPeriod, [$"TcuUi=s6next|{side}"]);
        Back(b);
        Panic(b, home: true);
        Letters(b, "START6");
    }

    // ── Strafe ───────────────────────────────────────────────────────────────
    void Penalty(DeckBuilder b, string side, int bg)
    {
        var target = side == "away" ? DeckContext.PlayerAway : DeckContext.PlayerHome;
        b.Action(0, 0, "2'",    bg, [$"TcuUi=prepare|penalty|{side}|2"],     TcuIcons.Strafe, target: target);
        b.Action(0, 1, "2'+2'", bg, [$"TcuUi=prepare|penalty|{side}|22"],    TcuIcons.Strafe, target: target);
        b.Action(0, 2, "10'",   bg, [$"TcuUi=prepare|penalty|{side}|10"],    TcuIcons.Strafe, target: target);
        b.Action(0, 3, "Match", bg, [$"TcuUi=prepare|penalty|{side}|match"], TcuIcons.Strafe, target: target);
        Back(b);
        Panic(b, home: true);
        Letters(b, "STRAFE");
    }

    // ── Meldung ──────────────────────────────────────────────────────────────
    void Message(DeckBuilder b)
    {
        b.Action(0, 0, "Technik", CProb, ["TcuUi=fault|technik"], TcuIcons.Meldung, target: DeckContext.Main, mode: "fault_technik");
        b.Action(0, 1, "Video",   CProb, ["TcuUi=fault|bild"],    TcuIcons.Meldung, target: DeckContext.Main, mode: "fault_bild");
        b.Action(0, 2, "Audio",   CProb, ["TcuUi=fault|ton"],     TcuIcons.Meldung, target: DeckContext.Main, mode: "fault_ton");

        b.Action(1, 0, "Highlights",                   CInfo, ["TcuUi=comment|highlights"], target: DeckContext.Main);
        b.Action(1, 1, "Leider heute\nohne Kommentar",  CInfo, ["TcuUi=comment|nocomment"],  target: DeckContext.Main);
        b.Action(1, 2, "Live aus der\nWin4 Stratos-Halle", CInfo, ["TcuUi=comment|live"],    target: DeckContext.Main);
        b.Action(2, 0, "Highlights\n1. Drittel",       CInfo, ["TcuUi=comment|hl1"],        target: DeckContext.Main);
        b.Action(2, 1, "Highlights\n2. Drittel",       CInfo, ["TcuUi=comment|hl2"],        target: DeckContext.Main);
        b.Action(2, 2, "Highlights\n3. Drittel",       CInfo, ["TcuUi=comment|hl3"],        target: DeckContext.Main);

        // Zuschauer: die Zählknöpfe bleiben im Kontext, weil dort mehrfach
        // hintereinander gedrückt wird. Erst "Anzeigen" ist der Abschluss.
        b.Action(0, 5, "+100", CCntP, ["TcuUi=spectators|100"]);
        b.Action(0, 6, "+10",  CCntP, ["TcuUi=spectators|10"]);
        b.Action(0, 7, "+1",   CCntP, ["TcuUi=spectators|1"]);
        b.Label (1, 6, $"Zuschauer\n{game.Spectators}", CNeutral);
        b.Action(1, 7, "👁 Anzeigen", CDisp, ["TcuUi=spectators|show"], target: DeckContext.Main, mode: "spectators");
        b.Action(2, 5, "-1",   CCntM, ["TcuUi=spectators|-1"]);
        b.Action(2, 6, "-10",  CCntM, ["TcuUi=spectators|-10"]);
        b.Action(2, 7, "-100", CCntM, ["TcuUi=spectators|-100"]);

        Back(b);
        Panic(b, home: true);
        Letters(b, "MELDUNG");
    }

    // ── Drittel ──────────────────────────────────────────────────────────────
    void Period(DeckBuilder b)
    {
        b.Action(0, 0, "1. Drittel", CPeriod, ["TcuController=scoreboard_period|1"], TcuIcons.Drittel, target: DeckContext.Main);
        b.Action(0, 1, "2. Drittel", CPeriod, ["TcuController=scoreboard_period|2"], TcuIcons.Drittel, target: DeckContext.Main);
        b.Action(0, 2, "3. Drittel", CPeriod, ["TcuController=scoreboard_period|3"], TcuIcons.Drittel, target: DeckContext.Main);
        b.Action(0, 4, "Overtime",   CPeriod, ["TcuController=scoreboard_period|O"], TcuIcons.Drittel, target: DeckContext.Main);
        b.Action(0, 5, "Penalty",    CPeriod, ["TcuController=scoreboard_period|P"], TcuIcons.Drittel, target: DeckContext.Main);
        b.Action(1, 5, "🥅 Penaltys\nzurücksetzen", CNeutral, ["TcuController=scoreboard_penaltyshots_reset"]);
        Back(b);
        Panic(b, home: true);
        Letters(b, "DRITTEL");
    }

    // ── Wiederkehrende Tasten ────────────────────────────────────────────────
    static void Back(DeckBuilder b) =>
        b.Nav(BackRow, BackCol, "◀ Zurück", CBack, DeckContext.Main);

    // PANIC kommt ohne die UI-Brücke aus: der Notaus soll auch dann greifen,
    // wenn im Ablauf etwas klemmt.
    void Panic(DeckBuilder b, bool home) =>
        b.Action(PanicRow, PanicCol, "🚨 PANIC", CPanic,
                 ["TcuController=lowerthird_hide",
                  "TcuController=scoreboard_hide",
                  "TcuController=sponsor_hide"],
                 target: home ? DeckContext.Main : null, mode: "", forceText: CWhite);

    static void Letters(DeckBuilder b, string word)
    {
        for (var i = 0; i < word.Length && i < 6; i++)
            b.Label(3, i + 1, word[i].ToString(), CLetter, forceText: CWhite);
    }

    static string Trunc(string s, int max) =>
        s.Length <= max ? s : s[..max];

    // ── Aufbauhilfe ──────────────────────────────────────────────────────────
    //
    // Sammelt die belegten Plätze und füllt am Ende den Rest mit leeren Tasten
    // auf. Das Modul bekommt dadurch immer genau 32 Einträge und muss selbst
    // nichts über fehlende Plätze wissen.
    sealed class DeckBuilder
    {
        readonly Dictionary<int, DeckSlot> _slots = [];

        static int Number(int row, int col) => row * Columns + col + 1;

        public void Action(int row, int col, string label, int bg, string[] cmds,
                           string? icon = null, DeckContext? target = null,
                           string? mode = null, int? forceText = null)
        {
            var fg = forceText ?? Fg(bg);
            _slots[Number(row, col)] = new DeckSlot(
                Number(row, col), label, ImageKey(icon, label, fg),
                bg, fg, Pressable: true, cmds, target, mode);
        }

        public void Nav(int row, int col, string label, int bg, DeckContext target, string? icon = null)
            => Action(row, col, label, bg, [], icon, target);

        public void Label(int row, int col, string label, int bg, int? forceText = null)
        {
            var fg = forceText ?? Fg(bg);
            _slots[Number(row, col)] = new DeckSlot(
                Number(row, col), label, null, bg, fg, Pressable: false, [], null, null);
        }

        public void Empty(int row, int col) =>
            _slots[Number(row, col)] = new DeckSlot(
                Number(row, col), "", null, CEmpty, CWhite, Pressable: false, [], null, null);

        /// <summary>Färbt die Taste des scharf gestellten Modus ein. Sie soll
        /// sofort erkennbar sein — sonst ist nach einem Kontextwechsel nicht
        /// mehr sichtbar, was der nächste Spielerdruck bewirkt.</summary>
        public void Highlight(string mode, int color)
        {
            if (mode.Length == 0) return;
            foreach (var key in _slots.Keys.ToList())
            {
                var s = _slots[key];
                if (s.Mode != mode) continue;
                var fg = Fg(color);
                _slots[key] = s with { Color = color, TextColor = fg, ImageKey = ReKey(s.ImageKey, fg) };
            }
        }

        static string? ReKey(string? key, int fg) =>
            key is null ? null : key[..key.LastIndexOf('|')] + $"|{fg:X6}";

        // Der Schlüssel ist genau das, was TcuIcons zum Aufbau braucht. Das
        // Modul holt das Bild einmal pro Schlüssel und merkt es sich — ohne das
        // gingen bei jedem Takt 32 Base64-Bilder über die Leitung.
        static string? ImageKey(string? icon, string label, int fg) =>
            icon is null ? null : $"{icon}|{label}|{fg:X6}";

        /// <summary>Schriftfarbe aus der Hintergrundhelligkeit, gewichtet nach
        /// Wahrnehmung (Rec. 601) — sonst gilt Blau als zu hell und Grün als zu
        /// dunkel.</summary>
        static int Fg(int bg)
        {
            int r = (bg >> 16) & 0xFF, g = (bg >> 8) & 0xFF, bl = bg & 0xFF;
            var luma = (0.299 * r + 0.587 * g + 0.114 * bl) / 255.0;
            return luma > 0.55 ? CDark : CWhite;
        }

        public List<DeckSlot> ToSlots()
        {
            var all = new List<DeckSlot>(SlotCount);
            for (var n = 1; n <= SlotCount; n++)
                all.Add(_slots.TryGetValue(n, out var s)
                    ? s
                    : new DeckSlot(n, "", null, CEmpty, CWhite, false, [], null, null));
            return all;
        }
    }
}

public enum DeckContext
{
    Main,
    PlayerHome, PlayerAway,
    GoalHome, GoalAway,
    Starting6Home, Starting6Away,
    PenaltyHome, PenaltyAway,
    Message,
    Period,
}

/// <summary>Eine Taste. <paramref name="Commands"/> und <paramref name="Target"/>
/// bleiben in TcuConsole — das Modul erfährt nur, wie die Taste aussieht.</summary>
public sealed record DeckSlot(
    int Number,
    string Label,
    string? ImageKey,
    int Color,
    int TextColor,
    bool Pressable,
    string[] Commands,
    DeckContext? Target,
    string? Mode)
{
    public int Color { get; set; } = Color;
    public int TextColor { get; set; } = TextColor;
    public string? ImageKey { get; set; } = ImageKey;
}
