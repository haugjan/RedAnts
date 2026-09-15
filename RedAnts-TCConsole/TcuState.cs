using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace TcuConsole;

/// <summary>
/// Liest den laufenden Zustand aus dem Fenster von TCunihockey und hält das
/// Stream Deck damit synchron.
///
/// Warum die Werte über Win32-Fenstertext und nicht über UI Automation kommen:
/// die Bedienelemente sind echte Kind-Fenster (212 Stück), ihr Inhalt steht als
/// Fenstertext bereit. Gemessen kostet ein Takt über alle Anker 0,03 ms, eine
/// UIA-Abfrage dagegen rund 3 ms je Element. UI Automation wird deshalb genau
/// einmal benutzt: um die Kennungen (AutomationId) den Fenster-Handles
/// zuzuordnen. Danach läuft alles über die Handles.
///
/// Ob eine Einblendung live ist, steht zwar auch in der Hintergrundfarbe der
/// Schaltfläche "Live ( / )" — an eine Farbe kommt man aber nur über einen
/// Bildabzug, und der zwingt TCunihockey zum Neuzeichnen; nach rund fünfzig
/// solchen Abzügen meldete es "Out of memory". Als Dauerabfrage untauglich.
///
/// Stattdessen wird die Sperre ausgewertet: solange etwas eingeblendet ist,
/// schaltet TCunihockey die 80 Bedienelemente inaktiv, die den Inhalt ändern
/// würden (gemessen durch Vergleich aller 212 Kind-Fenster zwischen beiden
/// Zuständen; im selben Zustand gemessen ändert sich nur die tickende Uhrzeit).
/// IsWindowEnabled kostet 0,09 µs und zwingt niemanden zum Neuzeichnen.
/// </summary>
public sealed class TcuState(TcuLogger logger)
{
    // ── Anker: Kennung im UIA-Baum → Fenster-Handle ──────────────────────────
    public const string IdPeriod     = "LblScoreboardPeriodValue";
    public const string IdClock      = "TxtScoreboardTime";
    public const string IdScoreHome  = "LblScoreboardScoreHome";
    public const string IdScoreAway  = "LblScoreboardScoreAway";
    public const string IdMessage    = "TxtShortcut14Line1";
    public const string IdSponsorLive = "BtnSponsorLive";

    // Mannschaftsnamen: daran wird erkannt, dass TCunihockey ein anderes Spiel
    // geladen hat. Kostet dasselbe wie jeder andere Text (Mikrosekunden) — der
    // Kader selbst ist nur über den Heap lesbar und braucht Sekunden, den holt
    // man sich also nicht im Takt, sondern erst wenn sich hier etwas ändert.
    public const string IdTeamHome = "LblHome";
    public const string IdTeamAway = "LblAway";

    // Die Spielernummern: Ziel des Rechtsklicks für das Eigentor-Menü.
    public const string IdNumberHome = "LblHomePlayerNumber";
    public const string IdNumberAway = "LblAwayPlayerNumber";

    /// <summary>Zähler der Starting Six, als ">>> (n/6)". Sichtbar nur, solange
    /// die Reihe läuft.</summary>
    public const string IdStarting6 = "BtnLiveNext";

    // Wächter für den Live-Zustand: diese drei gehören zu den 80 Elementen, die
    // TCunihockey sperrt, solange etwas eingeblendet ist. Bewusst drei aus
    // verschiedenen Bereichen und Mehrheitsentscheid — einzelne Knöpfe sind
    // auch aus anderen Gründen kurz gesperrt (gemessen: 20 ms nach dem
    // Ausblenden war der Heim-Bereich schon frei, Gast und Karten noch nicht).
    static readonly string[] LiveSentinels = ["BtnHomeGoal", "BtnAwayGoal", "BtnCardOpener"];

    static readonly string[] Anchors =
        [IdPeriod, IdClock, IdScoreHome, IdScoreAway, IdMessage, IdSponsorLive,
         IdNumberHome, IdNumberAway, IdTeamHome, IdTeamAway, .. LiveSentinels];

    readonly Dictionary<string, nint> _handles = new();
    nint _boundTo;   // Fenster, für das die Zuordnung gilt

    /// <summary>Was TCunihockey gerade anzeigt.</summary>
    public sealed record Snapshot(
        string Period, string Clock, string ScoreHome, string ScoreAway,
        bool LowerThirdLive, bool SponsorAuto, int? Spectators,
        string TeamHome, string TeamAway, bool ClockRunning = false)
    {
        /// <summary>Kennzeichen des geladenen Spiels. Ändert es sich, ist ein
        /// anderer Kader im Spiel.</summary>
        public string Match => $"{TeamHome} – {TeamAway}";

        public bool HasClock => Clock.Length > 0;

        /// <summary>Beschriftung des Drittel-Knopfs. TCunihockey liefert
        /// 1/2/3/O/P — gemessen über scoreboard_period|1..3,O,P.</summary>
        public string PeriodLabel => Period switch
        {
            "1" => "1. Drittel",
            "2" => "2. Drittel",
            "3" => "3. Drittel",
            "O" => "Overtime",
            "P" => "Penalty",
            ""  => "Drittel",
            var other => other,
        };
    }

    // ── Zuordnung herstellen ─────────────────────────────────────────────────
    // UI Automation kennt die Kennungen, liefert aber NativeWindowHandle = 0
    // (geprüft: für alle Anker). Die Brücke ist deshalb die Bildschirmlage:
    // jedes Element wird über sein Rechteck dem Kind-Fenster mit demselben
    // Rechteck zugeordnet. Das läuft einmal je Fenster, nicht je Takt.
    bool Bind(nint main)
    {
        if (_boundTo == main && _handles.Count > 0) return true;

        _handles.Clear();
        _boundTo = 0;

        var kids = new List<(nint H, int L, int T, int R, int B)>();
        EnumProc collect = (h, _) =>
        {
            GetWindowRect(h, out var r);
            kids.Add((h, r.L, r.T, r.R, r.B));

            // Der Starting-6-Zähler ist unsichtbar, solange keine Reihe läuft,
            // und taucht dann im UI-Automation-Baum überhaupt nicht auf — über
            // die Kennung ist er also nicht zu finden. Sein Fenstertext ist
            // dafür unverwechselbar: ">>> (n/6)".
            var t = new StringBuilder(64);
            GetWindowTextW(h, t, t.Capacity);
            if (t.Length > 3 && t[0] == '>' && t[1] == '>' && t[2] == '>')
                _handles[IdStarting6] = h;

            return true;
        };
        EnumChildWindows(main, collect, 0);
        GC.KeepAlive(collect);
        if (kids.Count == 0) return false;

        var began = Environment.TickCount64;
        try
        {
            // EIN Durchlauf über den Baum statt einer Suche je Kennung: eine
            // Einzelsuche kostet je nach Auslastung von TCunihockey bis zu einer
            // halben Sekunde, und sie skaliert mit der Zahl der Anker (gemessen:
            // 3641 ms für neun Einzelsuchen).
            var root  = AutomationElement.FromHandle(main);
            var wanted = new HashSet<string>(Anchors, StringComparer.Ordinal);

            foreach (AutomationElement el in root.FindAll(TreeScope.Descendants, Condition.TrueCondition))
            {
                string id;
                System.Windows.Rect b;
                try { id = el.Current.AutomationId ?? ""; b = el.Current.BoundingRectangle; }
                catch { continue; }
                if (!wanted.Contains(id) || _handles.ContainsKey(id)) continue;

                // 2 px Spiel: UIA rundet die Werte anders als GetWindowRect.
                var hit = kids.FirstOrDefault(k =>
                    Math.Abs(k.L - b.Left)  <= 2 && Math.Abs(k.T - b.Top)    <= 2 &&
                    Math.Abs(k.R - b.Right) <= 2 && Math.Abs(k.B - b.Bottom) <= 2);
                if (hit.H != 0) _handles[id] = hit.H;
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Zuordnung der Anzeigefelder fehlgeschlagen: {ex.Message}", LogLevel.Warning);
            return false;
        }

        if (_handles.Count == 0) return false;

        _boundTo = main;
        var missing = Anchors.Where(a => !_handles.ContainsKey(a)).ToList();
        logger.Log($"Anzeigefelder von TCunihockey zugeordnet: {_handles.Count}/{Anchors.Length} " +
                   $"in {Environment.TickCount64 - began} ms" +
                   (missing.Count > 0 ? $" — fehlt: {string.Join(", ", missing)}" : ""));
        return true;
    }

    // ── Lesen ────────────────────────────────────────────────────────────────
    string Text(string id)
    {
        if (!_handles.TryGetValue(id, out var h) || h == 0) return "";
        var sb = new StringBuilder(512);
        GetWindowTextW(h, sb, sb.Capacity);
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Text eines Feldes, das GetWindowTextW nicht herausgibt.
    ///
    /// Das Mitteilungsfeld ist ein RichEdit. GetWindowTextW liefert über
    /// Prozessgrenzen hinweg nur die Fenster-Beschriftung, und die ist bei
    /// RichEdit leer — gemessen: GetWindowTextW '' gegenüber WM_GETTEXT
    /// 'Zuschauerzahl: 110'. WM_GETTEXT wird vom System über die Prozessgrenze
    /// gereicht und liefert den Inhalt.
    /// </summary>
    string SentText(string id)
    {
        if (!_handles.TryGetValue(id, out var h) || h == 0) return "";
        var sb = new StringBuilder(1024);
        SendMessageTimeoutTxt(h, WM_GETTEXT, sb.Capacity, sb, SMTO_ABORTIFHUNG, 500, out _);
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Läuft gerade eine Einblendung? Ausgewertet über die Sperre der
    /// Bedienelemente, Mehrheit aus drei Wächtern.
    ///
    /// Eigene Methode neben Read(), weil die Ablaufsteuerung sie im
    /// Millisekundentakt abfragt: sie kostet 0,27 µs, ein voller Read() mit
    /// allen Texten 48 µs.
    /// </summary>
    public bool? LowerThirdLive()
    {
        var main = TcuWindow.Handle();
        if (main == 0 || !Bind(main)) return null;

        int locked = 0, known = 0;
        foreach (var id in LiveSentinels)
        {
            if (!_handles.TryGetValue(id, out var h) || h == 0) continue;
            known++;
            if (!IsWindowEnabled(h)) locked++;
        }
        return known == 0 ? null : locked * 2 > known;
    }

    /// <summary>
    /// Ist die Werbe-Automatik eingeschaltet? Das ist zugleich der Zustand, den
    /// der Werbungs-Knopf zeigt: TcuConsole schaltet Automatik und Einblendung
    /// immer gemeinsam.
    ///
    /// Gelesen wird über die Sperre der Schaltfläche "Live ( * )": bei laufender
    /// Automatik ist sie inaktiv, sonst bedienbar. Über zwölf Sekunden im
    /// Ruhezustand gemessen bleibt der Wert stabil.
    ///
    /// Das Kontrollkästchen "Auto" selbst taugt NICHT: BM_GETCHECK liefert
    /// prozessübergreifend immer 0, in beiden Zuständen — es ist eine
    /// WinForms-CheckBox in Knopf-Optik, deren Zustand nur im verwalteten
    /// Speicher steht.
    /// </summary>
    public bool? SponsorAuto()
    {
        var main = TcuWindow.Handle();
        if (main == 0 || !Bind(main)) return null;

        return _handles.TryGetValue(IdSponsorLive, out var liveH) && liveH != 0
            ? !IsWindowEnabled(liveH)
            : null;
    }

    /// <summary>Fenster-Handle eines Ankers, 0 wenn unbekannt. Für die
    /// Bedienung, die über blosses Lesen hinausgeht (Eigentor-Menü).</summary>
    public nint Handle(string id)
    {
        var main = TcuWindow.Handle();
        if (main == 0 || !Bind(main)) return 0;
        return _handles.TryGetValue(id, out var h) ? h : 0;
    }

    /// <summary>
    /// Stand der Starting Six: wie viele der sechs Namen schon gezeigt wurden.
    /// TCunihockey führt den Zähler im Knopf ">>> (n/6)".
    ///
    /// Gemessen: eingeblendet 1/6, danach je Weiter eins mehr bis 6/6; ein
    /// weiteres Weiter setzt auf 7/6 und blendet aus. Genau das soll nicht
    /// passieren, deshalb wird der Wert gebraucht.
    /// </summary>
    public (int Gezeigt, int Total)? Starting6()
    {
        var text = Text(IdStarting6);           // ">>> (3/6)"
        var auf  = text.IndexOf('(');
        var quer = text.IndexOf('/');
        var zu   = text.IndexOf(')');
        if (auf < 0 || quer < auf || zu < quer) return null;

        return int.TryParse(text[(auf + 1)..quer], out var n) &&
               int.TryParse(text[(quer + 1)..zu], out var total)
            ? (n, total)
            : null;
    }

    /// <summary>Momentaufnahme; null, wenn das Bedienfenster nicht erreichbar ist.
    ///
    /// ClockRunning bleibt hier false: ob die Uhr läuft, steht in keinem
    /// einzelnen Wert — das ergibt erst der Vergleich zweier Momentaufnahmen.
    /// Gesetzt wird es deshalb im Takt (RunAsync).</summary>
    public Snapshot? Read()
    {
        var main = TcuWindow.Handle();
        if (main == 0 || !Bind(main)) return null;

        return new Snapshot(
            Period:         Text(IdPeriod),
            Clock:          Text(IdClock),
            ScoreHome:      Text(IdScoreHome),
            ScoreAway:      Text(IdScoreAway),
            LowerThirdLive: LowerThirdLive() ?? false,
            SponsorAuto:    SponsorAuto() ?? false,
            Spectators:     ParseSpectators(SentText(IdMessage)),
            TeamHome:       Text(IdTeamHome),
            TeamAway:       Text(IdTeamAway));
    }

    /// <summary>Zuschauerzahl aus dem Mitteilungsfeld, falls dort eine steht.
    /// TCunihockey hat keinen Zähler — die Zahl ist eine freie Mitteilung, die
    /// TcuConsole selbst hineingeschrieben hat. Beim Start ist das die einzige
    /// Quelle, um nach einem Neustart weiterzuzählen statt bei 0 anzufangen.</summary>
    public static int? ParseSpectators(string message)
    {
        if (!message.StartsWith(TcuUi.SpectatorsPrefix, StringComparison.OrdinalIgnoreCase)) return null;
        var rest = message[TcuUi.SpectatorsPrefix.Length..].Trim();
        return int.TryParse(rest, out var n) ? n : null;
    }

    // ── Takt ─────────────────────────────────────────────────────────────────

    /// <summary>So lange darf die Anzeige stehen, bevor die Uhr als angehalten
    /// gilt. Die Uhr springt im Sekundentakt, abgetastet wird alle 500 ms — eine
    /// Änderung wird also spätestens 500 ms nach ihrem Eintreten gesehen, zwei
    /// aufeinanderfolgende liegen damit höchstens 1,5 s auseinander. 2 s lassen
    /// Luft für Taktschwankungen; ein knapperer Wert liesse den Knopf während
    /// des Spiels flackern.</summary>
    const int ClockIdleMs = 2000;

    /// <summary>Pollt und meldet nur Änderungen. Ein Takt kostet gemessen
    /// 0,03 ms, deshalb ist die Abtastrate unkritisch.</summary>
    public async Task RunAsync(Func<Snapshot, Task> onChange, CancellationToken token,
                               int intervalMs = 500)
    {
        string last = "";
        var warned = false;
        var dialogWarned = false;

        // Läuft die Uhr? TCunihockey sagt das nirgends — weder als Fenstertext
        // noch über die Sperre, die Einblendungen verrät. Ablesbar ist nur, DASS
        // sich die Anzeige ändert.
        string lastClock = "";
        long   clockMoved = 0;

        while (!token.IsCancellationRequested)
        {
            try
            {
                // Ein offenes Meldungsfenster blockiert TCunihockey vollständig:
                // UDP-Befehle werden angenommen, aber nicht abgearbeitet. Ohne
                // Hinweis sieht das aus, als wäre TcuConsole schuld.
                var dialogs = TcuWindow.Dialogs();
                if (dialogs.Count > 0)
                {
                    if (!dialogWarned)
                    {
                        dialogWarned = true;
                        logger.Log($"TCunihockey zeigt ein Meldungsfenster und reagiert nicht: " +
                                   $"\"{dialogs[0]}\" — bitte dort bestätigen.", LogLevel.Warning);
                    }
                }
                else dialogWarned = false;

                var s = Read();
                if (s is not null && s.HasClock)
                {
                    warned = false;

                    // Beim allerersten Takt nur merken, nicht stempeln: sonst
                    // gälte die Uhr für die erste Wartezeit als laufend, obwohl
                    // nichts beobachtet wurde. Im Zweifel steht sie.
                    var now = Environment.TickCount64;
                    if (lastClock.Length == 0) lastClock = s.Clock;
                    else if (s.Clock != lastClock) { lastClock = s.Clock; clockMoved = now; }

                    s = s with
                    {
                        ClockRunning = clockMoved != 0 && now - clockMoved < ClockIdleMs,
                    };

                    // Die Uhrzeit selbst steht bewusst NICHT im Vergleich: sie
                    // ändert sich jede Sekunde, wird aber nicht mehr aufs Deck
                    // geschrieben. Wäre sie dabei, liefe jede Sekunde ein
                    // Schwung Companion-Aufrufe für nichts. Ob sie LÄUFT, gehört
                    // dagegen hinein — das wechselt nur beim Anpfiff und beim
                    // Unterbruch und färbt den Uhr-Knopf.
                    var key = $"{s.ScoreHome}|{s.ScoreAway}|{s.Period}|" +
                              $"{s.LowerThirdLive}|{s.SponsorAuto}|{s.Spectators}|{s.Match}|" +
                              $"{s.ClockRunning}";
                    if (key != last)
                    {
                        last = key;
                        await onChange(s);
                    }
                }
                else if (!warned)
                {
                    warned = true;
                    last   = "";
                    logger.Log("Spielzustand nicht lesbar — läuft TCunihockey?", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Zustandsabfrage fehlgeschlagen: {ex.Message}", LogLevel.Warning);
            }

            try { await Task.Delay(intervalMs, token); }
            catch (OperationCanceledException) { break; }
        }
    }

    // ── Win32 ────────────────────────────────────────────────────────────────
    const int  WM_GETTEXT       = 0x000D;
    const uint SMTO_ABORTIFHUNG = 0x0002;

    delegate bool EnumProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")] static extern bool EnumChildWindows(nint hwnd, EnumProc cb, nint lParam);
    [DllImport("user32.dll")] static extern bool GetWindowRect(nint hwnd, out RECT r);
    [DllImport("user32.dll")] static extern bool IsWindowEnabled(nint hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowTextW(nint hwnd, StringBuilder buf, int max);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode)]
    static extern nint SendMessageTimeoutTxt(
        nint hwnd, int msg, nint w, StringBuilder buf, uint flags, uint timeout, out nint result);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int L, T, R, B; }
}
