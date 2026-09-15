using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace TcuConsole;

/// <summary>
/// Fernsteuerung der TCunihockey-Funktionen, für die es KEINEN UDP-Befehl gibt.
///
/// Belegt ist im Binary (v6.2.10) genau ein Befehlssatz von 51 "TcuController="
/// und 11 "TcuExternal="-Befehlen — als zusammenhängender Literal-Block im
/// String-Heap, andere Präfixe existieren nicht. Strafen, Karten, Shortcuts,
/// die Mitteilung unten links und "Highlight Resultat" kommen darin nicht vor.
/// Die gibt es ausschliesslich als Knopf im Fenster; diese Klasse drückt genau
/// diese Knöpfe.
///
/// Bewusst Win32 statt UI Automation, obwohl TcuStateReader für die Teamnamen
/// UIA verwendet: TCunihockey liefert im UIA-Baum zwar AutomationIds, aber
/// jedes Element kommt als [Pane] ohne Patterns — kein InvokePattern zum
/// Drücken, kein ValuePattern zum Setzen des Mitteilungstexts. Klicken ginge
/// darüber also ohnehin nicht. Dazu kostet ein FindFirst über TreeScope
/// .Descendants in diesem Fenster mehrere Sekunden, während EnumChildWindows
/// alle 212 Fenster in Millisekunden liefert — bei einem Knopfdruck während
/// des Spiels ist das der Unterschied zwischen brauchbar und unbrauchbar.
/// </summary>
public class TcuUi(TcuLogger logger)
{
    // ── Win32 ────────────────────────────────────────────────────────────────
    const int  BM_CLICK    = 0x00F5;
    const int  BM_GETCHECK = 0x00F0;
    const int  WM_SETTEXT  = 0x000C;
    const int  BST_CHECKED = 0x0001;
    const uint SMTO_ABORTIFHUNG = 0x0002;

    delegate bool EnumProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")]
    static extern bool EnumChildWindows(nint hwnd, EnumProc cb, nint lParam);
    [DllImport("user32.dll")]
    static extern nint GetParent(nint hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(nint hwnd, StringBuilder buf, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowTextW(nint hwnd, StringBuilder buf, int max);
    [DllImport("user32.dll")]
    static extern bool GetWindowRect(nint hwnd, out RECT r);
    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")]
    static extern bool IsWindowEnabled(nint hwnd);
    [DllImport("user32.dll")]
    static extern bool PostMessageW(nint hwnd, int msg, nint w, nint l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern nint SendMessageTimeoutW(
        nint hwnd, int msg, nint w, string l, uint flags, uint timeout, out nint result);
    [DllImport("user32.dll")]
    static extern nint SendMessageTimeoutW(
        nint hwnd, int msg, nint w, nint l, uint flags, uint timeout, out nint result);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int L, T, R, B; }

    // ── Ein Control im TCunihockey-Fenster ───────────────────────────────────
    public sealed record UiControl(
        nint Hwnd, nint Parent, string Class, string Text,
        int X, int Y, int W, int H, bool Visible, bool Enabled)
    {
        public bool IsButton => Class.StartsWith("WindowsForms10.BUTTON", StringComparison.Ordinal);
        public bool IsStatic => Class.StartsWith("WindowsForms10.STATIC", StringComparison.Ordinal);
        public bool IsEdit   => Class.StartsWith("WindowsForms10.EDIT",   StringComparison.Ordinal);

        /// <summary>Fenstertext ohne Zeilenumbrüche — TCunihockey beschriftet
        /// mehrzeilig ("Auf-\nstellung", "Starting\n6", "Time\nOut").</summary>
        public string Key => Text.Replace("\r", "").Replace("\n", "").Trim();

        public override string ToString() =>
            $"0x{Hwnd:X} {(IsButton ? "BUTTON" : IsEdit ? "EDIT" : IsStatic ? "STATIC" : "PANEL")} " +
            $"'{Key}' ({X},{Y} {W}x{H}) vis={Visible}";
    }

    /// <summary>Momentaufnahme aller Kind-Fenster samt Eltern-Zuordnung.</summary>
    public sealed class UiSnapshot
    {
        public required List<UiControl> All { get; init; }
        public required ILookup<nint, UiControl> ByParent { get; init; }

        public IEnumerable<UiControl> Children(nint panel) => ByParent[panel];

        /// <summary>Panel über die Beschriftung, die TCunihockey als Kopfzeile
        /// hineinlegt ("KARTEN", "ALLGEMEIN", "MATCHUHR", "Sponsoren").</summary>
        public nint PanelByHeader(string header) =>
            All.FirstOrDefault(c => c.IsStatic &&
                                    c.Key.Equals(header, StringComparison.OrdinalIgnoreCase))
               ?.Parent ?? 0;
    }

    // TCunihockey verarbeitet die Klicks auf seinem eigenen UI-Thread. Ohne
    // Pause zwischen den Schritten kommt der zweite Klick an, bevor das Panel
    // aus dem ersten überhaupt sichtbar ist (Strafe -> Dauer).
    const int StepDelayMs = 120;

    /// <summary>Text, den die Mitteilung unten links für die Zuschauerzahl
    /// bekommt. TCunihockey kennt keinen Zuschauerzähler — die Zahl ist nichts
    /// anderes als eine frei erfasste Mitteilung.</summary>
    public const string SpectatorsPrefix = "Zuschauerzahl: ";

    readonly object _lock = new();

    // ── Snapshot ─────────────────────────────────────────────────────────────
    public UiSnapshot? Snapshot()
    {
        // Fenstersuche über TcuWindow, nicht über MainWindowHandle: sobald
        // TCunihockey ein Meldungsfenster offen hat, zeigt MainWindowHandle auf
        // dieses — die Bedienelemente wären dann unauffindbar und jeder Klick
        // würde still verpuffen.
        var main = TcuWindow.Handle();
        if (main == 0)
        {
            logger.Log("Bedienfenster von TCunihockey nicht gefunden — UI-Befehl verworfen",
                       LogLevel.Warning);
            return null;
        }

        var handles = new List<nint>();
        // Der Delegate muss bis zum Ende von EnumChildWindows am Leben bleiben,
        // sonst sammelt der GC ihn mitten im Callback ein.
        EnumProc collect = (h, _) => { handles.Add(h); return true; };
        EnumChildWindows(main, collect, 0);
        GC.KeepAlive(collect);

        var cls = new StringBuilder(256);
        var txt = new StringBuilder(1024);
        var all = new List<UiControl>(handles.Count);

        foreach (var h in handles)
        {
            cls.Clear(); GetClassName(h, cls, cls.Capacity);
            txt.Clear(); GetWindowTextW(h, txt, txt.Capacity);
            GetWindowRect(h, out var r);
            all.Add(new UiControl(
                h, GetParent(h), cls.ToString(), txt.ToString(),
                r.L, r.T, r.R - r.L, r.B - r.T,
                IsWindowVisible(h), IsWindowEnabled(h)));
        }

        return new UiSnapshot { All = all, ByParent = all.ToLookup(c => c.Parent) };
    }

    // ── Layout: Mannschaftsbereiche ──────────────────────────────────────────
    // Die beiden Mannschaftsbereiche sind die Panels, die einen Knopf
    // "Aufstellung" enthalten. Bewusst nicht über "Tor": diesen Text tragen
    // auch die 20 Knöpfe des Penaltyschiessens.
    // Links ist Heim, rechts Gast — dieselbe Reihenfolge wie im Scoreboard.
    public (nint Home, nint Away) TeamPanels(UiSnapshot s)
    {
        var panels = s.All
            .Where(c => c.IsButton && c.Key.Equals("Auf-stellung", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Parent)
            .Distinct()
            .Select(p => (Panel: p, X: s.Children(p).Min(c => c.X)))
            .OrderBy(p => p.X)
            .ToList();

        return panels.Count == 2 ? (panels[0].Panel, panels[1].Panel) : (0, 0);
    }

    nint TeamPanel(UiSnapshot s, string side)
    {
        var (home, away) = TeamPanels(s);
        return side.Equals("away", StringComparison.OrdinalIgnoreCase) ? away : home;
    }

    static UiControl? Button(UiSnapshot s, nint panel, string key) =>
        s.Children(panel).FirstOrDefault(
            c => c.IsButton && c.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    // ── Layout: Karten ───────────────────────────────────────────────────────
    // Im KARTEN-Bereich stehen oben Opener / Highlight Resultat / Resultat,
    // darunter die acht frei beschriftbaren Karten. Die Karten werden über
    // ihre Lage bestimmt, nicht über den Text: ihre Beschriftung kommt aus der
    // System-Konfig (ButtonLabel) und ist damit vom Anwender änderbar.
    public List<UiControl> Cards(UiSnapshot s)
    {
        var panel = s.PanelByHeader("KARTEN");
        if (panel == 0) return [];

        var buttons = s.Children(panel).Where(c => c.IsButton).ToList();
        var result  = buttons.FirstOrDefault(
            c => c.Key.Equals("Resultat", StringComparison.OrdinalIgnoreCase));
        if (result is null) return [];

        return buttons.Where(c => c.Y > result.Y)
                      .OrderBy(c => c.Y).ThenBy(c => c.X)
                      .ToList();
    }

    // ── Layout: Bereich ALLGEMEIN (Shortcuts + Mitteilung) ───────────────────
    // Neun Shortcut-Knöpfe (Text aus der System-Konfig, leere sind unsichtbar),
    // dazu das Eingabefeld für die freie Mitteilung mit "Bearbeiten" und
    // "Vorschau". Die drei festen Bedienelemente heissen fix so — sie stehen
    // als deutsche Literale im Binary und sind nicht konfigurierbar.
    public sealed record AllgemeinArea(
        List<UiControl> Shortcuts, UiControl? Edit, UiControl? Bearbeiten, UiControl? Vorschau);

    public AllgemeinArea Allgemein(UiSnapshot s)
    {
        var panel = s.PanelByHeader("ALLGEMEIN");
        if (panel == 0) return new AllgemeinArea([], null, null, null);

        var children   = s.Children(panel).ToList();
        var bearbeiten = children.FirstOrDefault(c => c.IsButton && c.Key.Equals("Bearbeiten", StringComparison.OrdinalIgnoreCase));
        var vorschau   = children.FirstOrDefault(c => c.IsButton && c.Key.Equals("Vorschau",   StringComparison.OrdinalIgnoreCase));
        var edit       = children.FirstOrDefault(c => c.IsEdit);

        var shortcuts = children
            .Where(c => c.IsButton && c != bearbeiten && c != vorschau)
            .OrderBy(c => c.Y).ThenBy(c => c.X)
            .ToList();

        return new AllgemeinArea(shortcuts, edit, bearbeiten, vorschau);
    }

    // ── Aktionen ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Muss nach diesem Befehl eingeblendet werden?
    ///
    /// Das Einblenden darf NICHT in der Companion-Befehlskette stehen. Der
    /// Klick geht über TcuConsole und dessen UI-Thread; ein per Companion
    /// verzögertes lowerthird_show trifft TCunihockey unter Umständen früher
    /// als der Klick wirkt. Dann wird der alte Inhalt live geschaltet und der
    /// nachfolgende Klick nimmt ihn wieder von der Anzeige — genau das
    /// Verhalten, das beim Wechsel von Opener auf Highlight auftrat.
    /// Deshalb entscheidet hier der Befehl selbst, und die Brücke sendet das
    /// show erst nach erfolgreichem Klick.
    ///
    /// "penalty" ist bewusst ausgenommen: dort wird nur der Modus gesetzt,
    /// eingeblendet wird erst mit der Spielerwahl.
    /// </summary>
    public static bool NeedsShow(string command)
    {
        var body  = command.StartsWith("TcuUi=", StringComparison.OrdinalIgnoreCase)
            ? command["TcuUi=".Length..] : command;
        var parts = body.Split('|');
        var verb  = parts[0].Trim().ToLowerInvariant();
        var arg1  = parts.Length > 1 ? parts[1].Trim().ToLowerInvariant() : "";

        return verb switch
        {
            "card" or "shortcut" or "highlight_result" or "message" => true,
            "spectators" => arg1 == "show",   // reines Zählen bleibt unsichtbar
            _ => false,
        };
    }

    /// <summary>
    /// Führt einen "TcuUi=..."-Befehl aus. Rückgabe ist die Meldung fürs Log,
    /// null bei Erfolg ohne Anmerkung.
    /// </summary>
    public string? Execute(string command, TcuGameState state)
    {
        var body  = command.StartsWith("TcuUi=", StringComparison.OrdinalIgnoreCase)
            ? command["TcuUi=".Length..] : command;
        var parts = body.Split('|');
        var verb  = parts[0].Trim().ToLowerInvariant();
        var arg1  = parts.Length > 1 ? parts[1].Trim() : "";
        var arg2  = parts.Length > 2 ? string.Join('|', parts[2..]).Trim() : "";

        lock (_lock)
        {
            var s = Snapshot();
            if (s is null) return "TCunihockey läuft nicht";

            return verb switch
            {
                "team"             => TeamButton(s, arg1, arg2),
                "penalty"          => Penalty(s, arg1, arg2),
                "card"             => Card(s, arg1),
                "shortcut"         => Shortcut(s, arg1),
                "highlight_result" => ClickInCards(s, "Highlight Resultat"),
                "message"          => Message(s, arg1 + (arg2.Length > 0 ? "|" + arg2 : "")),
                "spectators"       => Spectators(s, state, arg1),
                _                  => $"Unbekannter UI-Befehl: {verb}",
            };
        }
    }

    /// <summary>
    /// Drückt einen Knopf im Mannschaftsbereich über seine Beschriftung —
    /// gebraucht für "Tor" und "+1".
    ///
    /// Diese beiden gibt es als UDP-Befehl nur zusammen
    /// (lowerthird_home_goal+1), also Modus und Torzählung in einem Schritt.
    /// Für die geforderte Reihenfolge — erst Spieler wählen, dann zählen —
    /// müssen sie einzeln gedrückt werden.
    ///
    /// "+1" ist unsichtbar, solange der Tor-Modus nicht scharf ist; ein Klick
    /// darauf würde wirkungslos verpuffen, deshalb die Sichtbarkeitsprüfung.
    /// Gesucht wird auch in Unter-Panels: die Knöpfe liegen je nach Funktion
    /// eine Ebene tiefer.
    /// </summary>
    string? TeamButton(UiSnapshot s, string side, string key)
    {
        var panel = TeamPanel(s, side);
        if (panel == 0) return "Mannschaftsbereiche nicht gefunden";

        var btn = Button(s, panel, key)
                  ?? s.Children(panel)
                      .Where(c => !c.IsButton)
                      .SelectMany(c => s.Children(c.Hwnd))
                      .FirstOrDefault(c => c.IsButton &&
                                           c.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

        if (btn is null)     return $"Knopf '{key}' im Bereich {side} nicht gefunden";
        if (!btn.Visible)    return $"Knopf '{key}' ist nicht sichtbar — Modus nicht scharf?";

        Click(btn);
        return null;
    }

    // Strafe: erst die Seite wählen (macht das verdeckte Dauer-Panel sichtbar),
    // dann die Dauer. Die Spielerwahl läuft danach wieder über UDP
    // (lowerthird_{side}_player|{nr}) — dieser Befehl wählt den Spieler für den
    // gerade eingestellten Modus, unabhängig davon, wie der Modus gesetzt wurde.
    string? Penalty(UiSnapshot s, string side, string duration)
    {
        var panel = TeamPanel(s, side);
        if (panel == 0) return "Mannschaftsbereiche nicht gefunden";

        var strafe = Button(s, panel, "Strafe");
        if (strafe is null) return "Knopf 'Strafe' nicht gefunden";

        var key = duration.ToLowerInvariant() switch
        {
            "2" or "120"         => "2'",
            "22" or "240" or "2+2" => "2+2'",
            "10" or "600"        => "10'",
            "match" or "m"       => "Match",
            _                    => "",
        };
        if (key.Length == 0) return $"Unbekannte Strafdauer: {duration}";

        Click(strafe);
        Thread.Sleep(StepDelayMs);

        // Das Dauer-Panel wird erst durch den Klick sichtbar — neu einlesen.
        var after = Snapshot();
        if (after is null) return "TCunihockey nicht mehr erreichbar";

        var panel2 = TeamPanel(after, side);
        var dur = after.Children(panel2)
            .Where(c => !c.IsButton)                       // Unter-Panel der Dauer-Knöpfe
            .SelectMany(c => after.Children(c.Hwnd))
            .FirstOrDefault(c => c.IsButton && c.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

        if (dur is null) return $"Strafdauer '{key}' nicht gefunden";
        if (!dur.Visible) return $"Strafdauer '{key}' ist nicht sichtbar — Strafe-Panel offen?";

        Click(dur);
        return null;
    }

    string? Card(UiSnapshot s, string index)
    {
        if (!int.TryParse(index, out var n) || n < 1) return $"Ungültige Karte: {index}";
        var cards = Cards(s);
        if (n > cards.Count) return $"Karte {n} existiert nicht ({cards.Count} vorhanden)";
        var card = cards[n - 1];
        if (!card.Visible) return $"Karte {n} ist unbelegt (in der System-Konfig ohne ButtonLabel)";
        Click(card);
        return null;
    }

    string? ClickInCards(UiSnapshot s, string key)
    {
        var panel = s.PanelByHeader("KARTEN");
        var btn   = panel == 0 ? null : Button(s, panel, key);
        if (btn is null) return $"Knopf '{key}' nicht gefunden";
        Click(btn);
        return null;
    }

    string? Shortcut(UiSnapshot s, string index)
    {
        if (!int.TryParse(index, out var n) || n < 1) return $"Ungültiger Shortcut: {index}";
        var area = Allgemein(s);
        if (n > area.Shortcuts.Count) return $"Shortcut {n} existiert nicht ({area.Shortcuts.Count} vorhanden)";
        var btn = area.Shortcuts[n - 1];
        if (!btn.Visible) return $"Shortcut {n} ist unbelegt (in der System-Konfig ohne Text)";
        Click(btn);
        return null;
    }

    /// <summary>
    /// Schreibt eine freie Mitteilung ins Feld unten links und übernimmt sie
    /// mit "Vorschau". Das Live-Schalten bleibt beim UDP-Befehl
    /// lowerthird_show, damit der Ablauf derselbe ist wie bei allen anderen
    /// Einblendungen.
    /// </summary>
    public string? Message(UiSnapshot s, string text)
    {
        var area = Allgemein(s);
        if (area.Edit is null)     return "Mitteilungsfeld nicht gefunden";
        if (area.Vorschau is null) return "Knopf 'Vorschau' nicht gefunden";

        // "Bearbeiten" ist eine CheckBox in Knopf-Optik: nur einschalten, wenn
        // sie aus ist — ein blindes Umschalten würde sie sonst ausschalten.
        if (area.Bearbeiten is not null && !IsChecked(area.Bearbeiten))
        {
            Click(area.Bearbeiten);
            Thread.Sleep(StepDelayMs);
        }

        if (!SetText(area.Edit, text)) return "Mitteilungstext konnte nicht gesetzt werden";
        Thread.Sleep(StepDelayMs);
        Click(area.Vorschau);
        return null;
    }

    string? Spectators(UiSnapshot s, TcuGameState state, string arg)
    {
        if (!arg.Equals("show", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(arg, System.Globalization.NumberStyles.AllowLeadingSign,
                              System.Globalization.CultureInfo.InvariantCulture, out var delta))
                return $"Ungültige Zuschauer-Änderung: {arg}";
            state.Spectators = Math.Max(0, state.Spectators + delta);
        }

        return Message(s, SpectatorsPrefix + state.Spectators);
    }

    // ── Primitive ────────────────────────────────────────────────────────────

    // PostMessage statt SendMessage: der Klick landet in der Nachrichten-
    // schlange von TCunihockey und blockiert diesen Prozess nicht, falls der
    // UI-Thread dort gerade mit dem Rendern beschäftigt ist.
    void Click(UiControl c)
    {
        logger.Log($"UI-Klick: {c}");
        PostMessageW(c.Hwnd, BM_CLICK, 0, 0);
    }

    bool IsChecked(UiControl c) =>
        SendMessageTimeoutW(c.Hwnd, BM_GETCHECK, 0, 0, SMTO_ABORTIFHUNG, 1000, out var r) != 0
        && r == BST_CHECKED;

    bool SetText(UiControl c, string text) =>
        SendMessageTimeoutW(c.Hwnd, WM_SETTEXT, 0, text, SMTO_ABORTIFHUNG, 1000, out _) != 0;

    /// <summary>
    /// Liest die Beschriftung der Shortcuts und Karten aus dem Fenster. Beides
    /// steht in der System-Konfig von TCunihockey, die TcuConsole nicht kennt —
    /// aus dem laufenden Fenster ist es die einzige verlässliche Quelle.
    /// Unbelegte Knöpfe sind dort unsichtbar und kommen als leerer Text.
    /// </summary>
    public bool ReadLabels(TcuGameState state)
    {
        var s = Snapshot();
        if (s is null) return false;

        var shortcuts = Allgemein(s).Shortcuts;
        var cards     = Cards(s);
        if (shortcuts.Count == 0 && cards.Count == 0) return false;

        state.Shortcuts = shortcuts.Select(c => c.Visible ? c.Key : "").ToList();
        state.Cards     = cards.Select(c => c.Visible ? c.Key : "").ToList();

        logger.Log($"TCunihockey-Oberfläche gelesen: " +
                   $"{state.Shortcuts.Count(t => t.Length > 0)} Shortcuts, " +
                   $"{state.Cards.Count(t => t.Length > 0)} Karten belegt");
        return true;
    }

    // ── Diagnose ─────────────────────────────────────────────────────────────
    // Findet alle Bedienelemente und meldet, was gefunden wurde — ohne einen
    // einzigen Klick. Gedacht zum Prüfen vor dem ersten Einsatz und nach jedem
    // TCunihockey-Update: ändert sich das Fensterlayout, fällt es hier auf.
    public object Probe()
    {
        var s = Snapshot();
        if (s is null) return new { ok = false, error = "TCunihockey läuft nicht" };

        var (home, away) = TeamPanels(s);
        var area  = Allgemein(s);
        var cards = Cards(s);

        object Team(nint panel, string label) => new
        {
            side   = label,
            found  = panel != 0,
            strafe = Button(s, panel, "Strafe")?.ToString(),
            dauer  = s.Children(panel)
                      .Where(c => !c.IsButton)
                      .SelectMany(c => s.Children(c.Hwnd))
                      .Where(c => c.IsButton)
                      .Select(c => c.Key)
                      .ToArray(),
        };

        return new
        {
            ok    = home != 0 && away != 0 && area.Edit is not null && cards.Count > 0,
            teams = new[] { Team(home, "home"), Team(away, "away") },
            karten = new
            {
                anzahl   = cards.Count,
                belegt   = cards.Where(c => c.Visible).Select(c => c.Key).ToArray(),
                highlight = s.PanelByHeader("KARTEN") is var p && p != 0
                            && Button(s, p, "Highlight Resultat") is not null,
            },
            allgemein = new
            {
                shortcuts  = area.Shortcuts.Count,
                // Nummer VOR dem Filtern vergeben — sie ist die Position in der
                // System-Konfig (Shortcut01..09), nicht die unter den belegten.
                belegt     = area.Shortcuts.Select((c, i) => (Nr: i + 1, c))
                                 .Where(x => x.c.Visible)
                                 .Select(x => $"{x.Nr}: {x.c.Key}").ToArray(),
                feld       = area.Edit?.ToString(),
                bearbeiten = area.Bearbeiten?.ToString(),
                vorschau   = area.Vorschau?.ToString(),
            },
        };
    }
}
