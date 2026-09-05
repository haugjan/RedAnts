using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TcuConsole;

/// <summary>
/// Generiert eine Companion 5.x .companionconfig (version 12, gzip).
///
/// Seiten:
///   1 Startseite        2 Spielerwahl Heim    3 Spielerwahl Gast
///   4 Starting 6 Heim   5 Starting 6 Gast
///   6 Strafe Heim       7 Strafe Gast
///   8 Meldung           9 Drittel
///  10 Tor Heim         11 Tor Gast
///
/// Zwei UDP-Verbindungen: "tcu" (7001) spricht TCunihockey direkt an, "tcuui"
/// (7002) die UI-Brücke in TcuConsole. Über die zweite laufen ausschliesslich
/// die Funktionen, für die TCunihockey keinen UDP-Befehl kennt — Strafen,
/// Karten, Shortcuts, Mitteilung und "Highlight Resultat". Der Rest bleibt
/// direkt und funktioniert auch ohne laufende TcuConsole.
///
/// Farbkonzept (RGB int):
///   Heim #FF7C80 · Gast #A6C9EC · Panic #FF0000 · Zurück #8ED973
///   Letter #2A2A2A · Period #D86DCD · CntP #C1F0C8 · CntM #FFCCCC
/// </summary>
public static class CompanionConfig
{
    // ── Farben ───────────────────────────────────────────────────────────────
    const int CHeimBg   = 0xFF7C80;
    const int CGastBg   = 0xA6C9EC;
    const int CPanic    = 0xFF0000;
    const int CBack     = 0x8ED973;
    const int CLetter   = 0x2A2A2A;
    const int CPeriod   = 0xD86DCD;
    const int CCntP     = 0xC1F0C8;
    const int CCntM     = 0xFFCCCC;
    const int CTimer    = 0xFF6600;
    const int CTog      = 0x47D359;
    const int CMeld     = 0xC1F0C8;
    const int CHighlit  = 0x00E5FF;
    const int CRes      = 0xF1A983;
    const int CNeutral  = 0x555555;
    // Helles Neutral für Icon-Knöpfe. Die Zeichnungen sind dunkel umrandet und
    // verschwinden auf CNeutral (0x555555) fast vollständig.
    const int CInfo     = 0xD9D9D9;
    // Farbe des aktiven Modus. Bewusst ein kräftiges Gelb: es kollidiert weder
    // mit Heim-Rot noch mit Gast-Blau, lässt die dunkel umrandeten Icons lesbar
    // und wirkt als "scharf gestellt".
    const int CActive   = 0xFFD400;
    const int COpener   = 0xD86DCD;
    const int CToutH    = 0x83E28E;
    const int CLblH     = 0xA6C9EC;
    const int CLblG     = 0xFF7C80;
    const int CWhite    = 0xFFFFFF;
    const int CDark     = 0x1A1A1A;
    const int CGray     = 0xADADAD;
    const int CProb     = 0xFF6600;
    const int CDisp     = 0xD86DCD;
    // Zustandsknöpfe (Einblender, Werbung): aus = grün, an = rot. Bewusst
    // dunkle Töne, damit die ins Bild gerechnete Beschriftung in beiden
    // Zuständen weiss bleiben kann — sie wechselt zur Laufzeit nicht mit.
    const int CStateOff = 0x1E7B34;
    const int CStateOn  = 0xC0201C;

    // Feste Plätze auf allen Unterseiten:
    //   Zurück oberhalb von PANIC, Sprechverbindung unten rechts.
    const int BackRow = 2, BackCol = 0;
    const int TalkRow = 3, TalkCol = 7;

    static int _id;
    static string Id() => $"a{_id++:x4}";
    static int _pgId;
    static string PgId() => $"pg{_pgId++:x6}";

    // ── Spieler-Raster ───────────────────────────────────────────────────────
    // Einzige Quelle für die Belegung der Spielerwahl-Seiten. Sowohl der
    // Config-Generator (P2_Spielerwahl) als auch der Live-Label-Push
    // (CompanionPush) gehen hierüber — sonst driften die beiden Raster
    // auseinander und der Push überschreibt fremde Buttons.
    public const int PlayerPageHome = 2;
    public const int PlayerPageAway = 3;

    // Anzeigefelder, die TcuConsole laufend beschriftet. Sie müssen mit den
    // Buttons in P1_Startseite/P8_Meldung übereinstimmen — deshalb hier als
    // Konstanten, damit Generator und Push nicht auseinanderlaufen.
    //
    // Alle diese Knöpfe sind bewusst OHNE Bild angelegt: bei bebilderten
    // Knöpfen steckt die Beschriftung im Bild und liesse sich zur Laufzeit
    // nicht mehr ändern.
    public const int MainPage  = 1;

    // Drittel steht links vom Heimstand — Drittel, Stand Heim, Stand Gast in
    // einer Reihe, in der Reihenfolge, in der man sie liest.
    public const int PeriodRow = 2;
    public const int PeriodCol = 2;

    // Jeder Stand steht unter dem Starting-6-Knopf seiner Mannschaft und trägt
    // deren Farbe: Heim links (Zeile 1, Spalte 3), Gast rechts (Spalte 4).
    public const int ScoreHomeRow = 2, ScoreHomeCol = 3;
    public const int ScoreAwayRow = 2, ScoreAwayCol = 4;

    // Spieluhr: Start/Stop unter dem Gaststand, die Sekundenkorrektur links
    // daneben. Beides in der Zeile unter dem Spielstand, wo vorher das Drittel
    // sass — das ist jetzt eine Reihe höher.
    public const int ClockRow    = 3, ClockCol    = 4;
    public const int ClockBackRow = 3, ClockBackCol = 3;

    public const int SpectatorsPage = 8;
    public const int SpectatorsRow  = 1;
    public const int SpectatorsCol  = 6;

    // Seiten mit Spielerraster — dorthin geht der Namens-Push.
    public const int PlayerPageTorHome = 10;
    public const int PlayerPageTorAway = 11;

    public const int Starting6PageHome = 4;
    public const int Starting6PageAway = 5;

    /// <summary>Kopfzeile der Spielerseiten — dort schreibt TcuConsole hinein,
    /// welcher Modus gerade scharf ist.</summary>
    public static (int Row, int Col) HeadPos => (HeadRow, HeadCol);

    /// <summary>Die vier Seiten mit Spielerraster: Seite und ob es eine
    /// Tor-Seite ist (die hat einen Platz weniger, dort steht Eigentor).</summary>
    public static IEnumerable<(int Page, bool Tor, bool Home)> PlayerPages()
    {
        yield return (PlayerPageHome,    false, true);
        yield return (PlayerPageAway,    false, false);
        yield return (PlayerPageTorHome, true,  true);
        yield return (PlayerPageTorAway, true,  false);
    }
    const int PlayerCols = 8;   // Spalten 0..7

    // Belegung der Spielerseiten:
    //   Zeilen 0–2  ganz für Spieler, ausser [2,0] — dort sitzt "Zurück"
    //   Zeile 3     PANIC | Kopfzeile | (Eigentor) | Spieler … | Sprechverbindung
    //
    // Die sechs Buchstabenknöpfe der unteren Reihe sind weg. Sie waren reine
    // Zierde, und der Platz wurde knapp: der Heimkader des Beispielspiels hat
    // 23 Einträge und füllte die 23 Plätze exakt aus — ein Spieler mehr wäre
    // weggefallen. Jetzt sind es 27 bzw. 28.
    const int HeadRow = 3, HeadCol = 1;   // Kopfzeile: zeigt den gewählten Modus
    const int OwnGoalCol = 2;             // nur auf den Tor-Seiten belegt

    /// <summary>Zahl der Plätze. Auf den Tor-Seiten einer weniger, weil dort
    /// der Eigentor-Knopf steht.</summary>
    public static int PlayerSlotCount(bool tor) => tor ? 27 : 28;

    /// <summary>
    /// Alle Plätze des Spielerrasters — IMMER alle, auch die, für die der
    /// aktuelle Kader keinen Spieler hat.
    ///
    /// Das ist der Kern der Kaderunabhängigkeit: die Config legt für jeden
    /// Platz einen Knopf an, der seinen Platz meldet ("#7"), nicht eine
    /// Spielernummer. Beschriftung und Belegung kommen zur Laufzeit aus dem
    /// Push. Ein neuer Kader braucht deshalb keinen neuen Import — vorher stand
    /// die Nummer fest im Knopf, und der Push änderte nur den Text: der Knopf
    /// zeigte dann den neuen Namen und blendete den alten Spieler ein.
    /// </summary>
    public static IEnumerable<(int Row, int Col, int Index, Player? Player, string Label)> PlayerSlots(
        List<Player> players, bool tor)
    {
        var i = 0;
        foreach (var (row, col) in Positions(tor))
        {
            var p = i < players.Count ? players[i] : null;
            yield return (row, col, i, p, p is null ? "" : $"{p.Display}\n{Trunc(p.Name, 10)}");
            i++;
        }
    }

    static IEnumerable<(int Row, int Col)> Positions(bool tor)
    {
        for (var row = 0; row <= 3; row++)
            for (var col = 0; col < PlayerCols; col++)
            {
                if (row == BackRow && col == BackCol) continue;          // Zurück
                if (row == 3 && (col == 0 || col == HeadCol)) continue;  // PANIC, Kopfzeile
                if (row == 3 && col == TalkCol) continue;                // Sprechverbindung
                if (row == 3 && col == OwnGoalCol && tor) continue;      // Eigentor
                yield return (row, col);
            }
    }

    /// <summary>Die sechs Namensfelder der Starting-6-Seiten. Ebenfalls immer
    /// alle sechs, damit der Push sie beschriften und leeren kann.</summary>
    public const int Starting6Row = 0;
    public const int Starting6Count = 6;

    public static IEnumerable<(int Col, Player? Player, string Label)> Starting6Slots(
        List<Player> starting6)
    {
        for (var i = 0; i < Starting6Count; i++)
        {
            var p = i < starting6.Count ? starting6[i] : null;
            yield return (i, p, p is null ? "" : $"{p.Display}\n{Trunc(p.Name, 10)}");
        }
    }

    // ── Einstiegspunkt ───────────────────────────────────────────────────────
    // Schreibt die Config als Datei und liefert den absoluten Pfad zurück.
    public static string WriteToFile(TcuGameState state, string path)
    {
        var full = Path.GetFullPath(path);
        var dir  = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(full, Generate(state));
        return full;
    }

    // _id/_pgId sind statisch und werden hier zurückgesetzt — Generate muss
    // deshalb serialisiert laufen, sonst mischen sich parallele Aufrufe
    // (HTTP-Download und Datei-Export) und erzeugen doppelte Action-IDs.
    static readonly object _genLock = new();

    public static byte[] Generate(TcuGameState state)
    {
        lock (_genLock)
        {
        _id = 0; _pgId = 0;

        // Bewusst Format-Version 6 statt 12.
        //
        // Companion 5 liest eine als "12" deklarierte Datei als bereits
        // aktuell und überspringt sämtliche Upgrade-Schritte. Genau diese
        // Schritte übersetzen aber die Felder, die wir schreiben:
        //   instance_type   -> moduleId          (Stufe 10, Funktion kBe)
        //   {instance,action} -> {connectionId,definitionId}  (Funktion NBe)
        //   controller      -> surfaceId         (set_page-Migration)
        // Mit "12" kam die Modul-ID als undefined an ("Unknown module") und
        // die Actions wurden komplett verworfen. Mit "6" erledigt Companion
        // die Umwandlung selbst — das ist getesteter Code und deutlich
        // robuster, als hier drei interne Schemata nachzubauen.
        var root = new JsonObject
        {
            ["version"]          = 6,
            ["type"]             = "full",
            ["instances"]        = Instances(),
            ["custom_variables"] = CustomVariables(),
            ["pages"]            = Pages(state),
        };

        var json  = root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        var bytes = Encoding.UTF8.GetBytes(json);
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Fastest))
            gz.Write(bytes, 0, bytes.Length);
        return ms.ToArray();
        }
    }

    // ── UDP-Instanz ──────────────────────────────────────────────────────────
    // Modul-ID, Config-Keys und Action-Optionen müssen exakt zu
    // companion-module-generic-tcp-udp passen (manifest.json: id
    // "generic-tcp-udp"; Config: prot/host/port; Action "send" mit
    // id_send/id_end).
    //
    // "instance_type" ist hier korrekt, WEIL oben version 6 deklariert wird —
    // Companion übersetzt das Feld beim Import selbst nach "moduleId".
    // Wer die Version hochzieht, muss zugleich auf moduleId /
    // moduleVersionId / moduleInstanceType / updatePolicy umstellen, sonst
    // meldet Companion die Verbindung als "Unknown module".
    public const string ModuleId = "generic-tcp-udp";

    // Zwei Verbindungen, beide über dasselbe Modul:
    //   "tcu"   → 7001, TCunihockey selbst. Funktioniert ohne TcuConsole.
    //   "tcuui" → 7002, die UI-Brücke in TcuConsole. Nur für die Funktionen,
    //             für die TCunihockey keinen UDP-Befehl kennt (Strafen, Karten,
    //             Shortcuts, Mitteilung, Highlight Resultat) — die werden dort
    //             als Klick im Fenster ausgeführt.
    static JsonObject Instances() => new()
    {
        ["tcu"]   = Udp("TCU",    7001),
        ["tcuui"] = Udp("TCU UI", TcuUiBridge.Port),
    };

    // ── Optisches Feedback: welcher Modus ist gewählt? ───────────────────────
    // Jeder Modus-Knopf schreibt beim Drücken seinen Namen in die
    // Companion-Variable "tcu_mode" und trägt ein Feedback, das auf genau
    // diesen Namen prüft und den Knopf einfärbt. Damit leuchtet immer genau
    // ein Knopf, ohne dass TcuConsole beteiligt sein muss — Companion führt
    // den Zustand selbst.
    //
    // Referenziert wird die Variable als "custom:tcu_mode"; Companion legt
    // Custom-Variablen unter dem Label "custom" ab.
    public const string ModeVariable = "tcu_mode";

    // Zustandsvariablen. Geschrieben werden sie NICHT mehr vom Knopf selbst,
    // sondern von TcuConsole (CompanionPush.SetVariableAsync). Der Knopf trägt
    // nur noch ein Feedback darauf.
    //
    // Vorher führte der Umschalter seinen Zustand über zwei Schritte, die
    // abwechselnd "1" und "0" schrieben. Das läuft zwangsläufig aus dem Tritt,
    // sobald irgendwo anders geschaltet wird — am TCunihockey-Fenster, über
    // einen anderen Knopf oder über PANIC. Der Knopf zeigte dann grün, obwohl
    // etwas lief, und schaltete beim Druck genau verkehrt herum.
    public const string LowerThirdVariable  = "tcu_lt_live";
    public const string SponsorLiveVariable = "tcu_sponsor_live";
    public const string SponsorAutoVariable = "tcu_sponsor_auto";
    public const string ClockRunningVariable = "tcu_clock_running";

    static JsonObject CustomVariables() => new()
    {
        [ModeVariable] = new JsonObject
        {
            ["description"]         = "Aktuell gewählter TCU-Modus (optisches Feedback)",
            ["defaultValue"]        = "",
            ["persistCurrentValue"] = false,
            ["sortOrder"]           = 0,
        },
        [LowerThirdVariable] = new JsonObject
        {
            ["description"]         = "Einblendung läuft (1) oder nicht (0) — von TcuConsole gesetzt",
            ["defaultValue"]        = "0",
            ["persistCurrentValue"] = false,
            ["sortOrder"]           = 1,
        },
        [SponsorLiveVariable] = new JsonObject
        {
            ["description"]         = "Werbung läuft (1) oder nicht (0) — von TcuConsole gesetzt",
            ["defaultValue"]        = "0",
            ["persistCurrentValue"] = false,
            ["sortOrder"]           = 2,
        },
        [SponsorAutoVariable] = new JsonObject
        {
            ["description"]         = "Werbung Auto eingeschaltet (1) oder nicht (0) — aus TCunihockey gelesen",
            ["defaultValue"]        = "0",
            ["persistCurrentValue"] = false,
            ["sortOrder"]           = 3,
        },
        [ClockRunningVariable] = new JsonObject
        {
            ["description"]         = "Spieluhr läuft (1) oder steht (0) — von TcuConsole gesetzt",
            ["defaultValue"]        = "0",
            ["persistCurrentValue"] = false,
            ["sortOrder"]           = 4,
        },
    };

    static JsonArray VarFeedback(string name, string value, int bg) => new()
    {
        new JsonObject
        {
            ["id"]          = Id(),
            ["type"]        = "variable_value",
            ["instance_id"] = "internal",
            ["options"]     = new JsonObject
            {
                ["variable"] = $"custom:{name}",
                ["op"]       = "eq",
                ["value"]    = value,
            },
            ["style"] = new JsonObject { ["bgcolor"] = bg, ["color"] = Fg(bg) },
        },
    };

    static JsonObject ModeAction(string mode) => new()
    {
        ["id"]       = Id(),
        ["instance"] = "internal",
        ["action"]   = "custom_variable_set_value",
        ["options"]  = new JsonObject { ["name"] = ModeVariable, ["value"] = mode },
        ["delay"]    = 0,
        ["disabled"] = false,
    };

    static JsonArray ModeFeedback(string mode, int activeBg) => new()
    {
        new JsonObject
        {
            ["id"]          = Id(),
            ["type"]        = "variable_value",
            ["instance_id"] = "internal",
            ["options"]     = new JsonObject
            {
                ["variable"] = $"custom:{ModeVariable}",
                ["op"]       = "eq",
                ["value"]    = mode,
            },
            ["style"]       = new JsonObject
            {
                ["bgcolor"] = activeBg,
                ["color"]   = Fg(activeBg),
            },
        },
    };

    static JsonObject Udp(string label, int port) => new()
    {
        ["instance_type"]    = ModuleId,
        ["label"]            = label,
        ["enabled"]          = true,
        ["isFirstInit"]      = false,
        ["lastUpgradeIndex"] = -1,
        ["config"]           = new JsonObject
        {
            ["host"] = "127.0.0.1",
            ["port"] = port,
            ["prot"] = "udp",          // Modul unterscheidet tcp/udp über dieses Feld
        },
    };

    // ── Seiten 1–11 ──────────────────────────────────────────────────────────
    // Die Matchuhr hat keine eigene Seite mehr: gebraucht wird von ihr genau
    // ein Knopf — Start/Stop —, und der steht auf der Startseite.
    static JsonObject Pages(TcuGameState s) => new()
    {
        ["1"]  = Page("Startseite",       P1_Startseite()),
        ["2"]  = Page("Spielerwahl Heim", P2_Spielerwahl(s.HomePlayers, "home", CHeimBg, tor: false)),
        ["3"]  = Page("Spielerwahl Gast", P2_Spielerwahl(s.AwayPlayers, "away", CGastBg, tor: false)),
        ["4"]  = Page("Starting 6 Heim",  P4_Starting6(s.Starting6("home"), "home", CHeimBg)),
        ["5"]  = Page("Starting 6 Gast",  P4_Starting6(s.Starting6("away"), "away", CGastBg)),
        ["6"]  = Page("Strafe Heim",      P6_Strafe("home", CHeimBg)),
        ["7"]  = Page("Strafe Gast",      P6_Strafe("away", CGastBg)),
        ["8"]  = Page("Meldung",          P8_Meldung()),
        ["9"]  = Page("Drittel",          P9_Drittel()),
        // Eigene Seiten für das Tor: nur dort gehört der Eigentor-Knopf hin,
        // bei Strafe und Spielerwahl wäre er sinnlos.
        ["10"] = Page("Tor Heim",         P2_Spielerwahl(s.HomePlayers, "home", CHeimBg, tor: true)),
        ["11"] = Page("Tor Gast",         P2_Spielerwahl(s.AwayPlayers, "away", CGastBg, tor: true)),
    };

    // ── Seite 1: Startseite ──────────────────────────────────────────────────
    //
    // Jede Einblendung ist genau EIN Befehl an TcuConsole ("TcuUi=..."); den
    // Ablauf — ausblenden, warten, umschalten, einblenden — fährt TcuLower.
    //
    // Vorher stand die ganze Kette hier, mit festen Verzögerungen von 50 ms.
    // Das konnte nicht funktionieren: TCunihockey nimmt keinen Moduswechsel an,
    // solange etwas läuft, und das Ausblenden dauert 2,2 Sekunden. Gemessen war
    // ein Wechsel mit 1500 ms Wartezeit in 0 von 3 Versuchen erfolgreich, ab
    // 2000 ms in 3 von 3. Eine feste Wartezeit auf jedem Knopf wäre aber
    // unzumutbar — deshalb entscheidet TcuConsole anhand des gelesenen Zustands.
    //
    // Tor/Spieler/Best Player/Strafe stellen nur den Modus scharf und
    // navigieren zur Spielerwahl; eingeblendet wird erst mit dem Spieler.
    static Buttons P1_Startseite()
    {
        var b = new Buttons();

        // Zeile 0 — Aktionen Heim | Gast
        // Beschriftung einzeilig, ohne "Heim"/"Gast": die Seite ergibt sich aus
        // der Farbe (rot links, blau rechts) und aus der Position. Das Motiv
        // belegt die oberen zwei Drittel, der Text steht darunter.
        b[0, 0] = MultiBtn("Tor",         CHeimBg, ["TcuUi=prepare|goal|home"],  navPage: PlayerPageTorHome, icon: TcuIcons.Tor, mode: "tor_home");
        b[0, 1] = UdpBtn  ("Coach",       CHeimBg, ["TcuUi=lt|coach_home"],                 icon: TcuIcons.Coach,       mode: "coach_home");
        b[0, 2] = UdpBtn  ("Timeout",     CHeimBg, ["TcuUi=lt|timeout_home"],               icon: TcuIcons.Timeout,     mode: "timeout_home");
        b[0, 3] = UdpBtn  ("Aufstellung", CHeimBg, ["TcuUi=lt|lineup_home"],                icon: TcuIcons.Aufstellung, mode: "lineup_home");
        b[0, 4] = UdpBtn  ("Aufstellung", CGastBg, ["TcuUi=lt|lineup_away"],                icon: TcuIcons.Aufstellung, mode: "lineup_away");
        b[0, 5] = UdpBtn  ("Timeout",     CGastBg, ["TcuUi=lt|timeout_away"],               icon: TcuIcons.Timeout,     mode: "timeout_away");
        b[0, 6] = UdpBtn  ("Coach",       CGastBg, ["TcuUi=lt|coach_away"],                 icon: TcuIcons.Coach,       mode: "coach_away");
        b[0, 7] = MultiBtn("Tor",         CGastBg, ["TcuUi=prepare|goal|away"],  navPage: PlayerPageTorAway, icon: TcuIcons.Tor, mode: "tor_away");

        // Zeile 1 — Strafe | Spieler | Best Player | Starting 6
        // Strafe ist reine Navigation; die Dauer wird erst auf der Zielseite
        // gewählt. Starting 6 setzt den Modus und geht zur Namensübersicht.
        b[1, 0] = NavBtn  ("Strafe",      CHeimBg, 6,                                       icon: TcuIcons.Strafe);
        b[1, 1] = MultiBtn("Spieler",     CHeimBg, ["TcuUi=prepare|name|home"],  navPage: 2, icon: TcuIcons.Spieler,    mode: "name_home");
        b[1, 2] = MultiBtn("Best Player", CHeimBg, ["TcuUi=prepare|best|home"],  navPage: 2, icon: TcuIcons.BestPlayer, mode: "best_home");
        b[1, 3] = MultiBtn("Starting 6",  CHeimBg, ["TcuUi=s6|home"],            navPage: 4, icon: TcuIcons.Starting6,  mode: "s6_home");
        b[1, 4] = MultiBtn("Starting 6",  CGastBg, ["TcuUi=s6|away"],            navPage: 5, icon: TcuIcons.Starting6,  mode: "s6_away");
        b[1, 5] = MultiBtn("Best Player", CGastBg, ["TcuUi=prepare|best|away"],  navPage: 3, icon: TcuIcons.BestPlayer, mode: "best_away");
        b[1, 6] = MultiBtn("Spieler",     CGastBg, ["TcuUi=prepare|name|away"],  navPage: 3, icon: TcuIcons.Spieler,    mode: "name_away");
        b[1, 7] = NavBtn  ("Strafe",      CGastBg, 7,                                       icon: TcuIcons.Strafe);

        // Zeile 2 — Meldung | Spielstand | Grafiksteuerung
        // Das Feld über PANIC bleibt hier frei: auf den Unterseiten sitzt dort
        // der Zurück-Knopf, und auf der Startseite gibt es nichts, wohin.
        b[2, 1] = NavBtn  ("Meldung",                   CMeld,    8, icon: TcuIcons.Meldung);
        // Drittel: zeigt das laufende Drittel (von TcuConsole geschrieben) und
        // führt zur Drittelwahl. Bewusst ohne Bild — bei bebilderten Knöpfen
        // steckt die Beschriftung im Bild und liesse sich nicht mehr ändern.
        //
        // Steht links vom Heimstand: Drittel, Stand Heim, Stand Gast bilden
        // damit eine zusammenhängende Anzeige des Spielstands.
        b[PeriodRow, PeriodCol] = NavBtn("Drittel", CPeriod, 9);
        // Spielstand: jede Zahl steht unter dem Starting-6-Knopf ihrer
        // Mannschaft und trägt deren Farbe — Heim links, Gast rechts. Die
        // Zahlen schreibt TcuConsole laufend hinein; beschriftet sind sie
        // nicht, die Seite ergibt sich wie überall hier aus der Farbe.
        b[ScoreHomeRow, ScoreHomeCol] = LabelBtn("–", CHeimBg);
        b[ScoreAwayRow, ScoreAwayCol] = LabelBtn("–", CGastBg);
        // "Highlight Resultat" gibt es nur als Knopf im Fenster — kein
        // UDP-Befehl. Läuft wie alle Einblendungen über die Brücke.
        b[2, 5] = UdpBtn  ("Highlight",                 CHighlit, ["TcuUi=highlight"],   icon: TcuIcons.Highlight,      mode: "highlight");
        b[2, 6] = UdpBtn  ("Resultat",                  CRes,     ["TcuUi=lt|result"],   icon: TcuIcons.Resultat,       mode: "result");
        b[2, 7] = UdpBtn  ("Opener",                    COpener,  ["TcuUi=lt|opener"],   icon: TcuIcons.Opener,         mode: "opener");

        // Zeile 3 — System
        b[3, 0] = PanicBtn(navigateHome: false);
        b[3, 1] = UdpBtn  ("Kommentar",                 CInfo,    ["TcuUi=lt|commentary"], icon: TcuIcons.Kommentar,      mode: "commentary");
        b[3, 2] = UdpBtn  ("Schiri",                    CInfo,    ["TcuUi=lt|referee"],    icon: TcuIcons.Schiedsrichter, mode: "referee");
        // Spieluhr: Sekunde zurück und Start/Stop, nebeneinander unter dem
        // Spielstand. Die Uhrzeit selbst steht weiterhin nicht auf dem Deck —
        // sie ändert sich jede Sekunde und wäre ein Dauerfeuer an
        // Companion-Aufrufen für etwas, das im TCunihockey-Fenster ohnehin
        // gross dasteht.
        b[ClockBackRow, ClockBackCol] = ClockBackBtn();
        b[ClockRow,     ClockCol]     = ClockBtn();
        // Werbung: schaltet Automatik und Einblendung gemeinsam, in der
        // Reihenfolge, die zum aktuellen Zustand passt (TcuLower liest ihn).
        b[3, 5] = StateBtn("Werbung",    SponsorLiveVariable, ["TcuUi=sponsor|toggle"],
                           icon: TcuIcons.Werbung);
        // Einblender steuert den LOWER THIRD, nicht die Matchuhr.
        //
        // Kein lowerthird_toggleshowhide mehr: der zählt in TCunihockey seinen
        // eigenen Zustand mit und schaltet verkehrt herum, sobald der von dem
        // abweicht, was tatsächlich läuft. TcuLower liest stattdessen den
        // Zustand und sendet gezielt show oder hide.
        b[3, 6] = StateBtn("Einblender", LowerThirdVariable, ["TcuUi=toggle"],
                           icon: TcuIcons.EinblenderEin);
        b[TalkRow, TalkCol] = TalkBtn();

        return b;
    }

    /// <summary>
    /// Sprechverbindung. Hat mit TCunihockey nichts zu tun und ist noch ohne
    /// Funktion — der Knopf steht bereits am Platz, damit die Belegung sich
    /// später nicht mehr verschiebt.
    /// </summary>
    static JsonObject TalkBtn() => LabelBtn("Sprechverb.", CInfo, icon: TcuIcons.Sprech);

    /// <summary>Zurück zur Startseite, auf jeder Unterseite an derselben
    /// Stelle: oberhalb von PANIC.</summary>
    static JsonObject BackBtn() => NavBtn("◀ Zurück", CBack, 1);

    // ── Seiten 2–3: Spielerwahl ──────────────────────────────────────────────
    // Der Spielerknopf meldet nur "dieser Spieler" an TcuConsole; was damit
    // geschieht, hängt davon ab, was vorher scharf gestellt wurde — Tor, Name,
    // Best Player oder Strafe. Nur so lässt sich die geforderte Reihenfolge
    // beim Tor einhalten: erst Spieler setzen, dann den Stand erhöhen, dann
    // einblenden.
    static Buttons P2_Spielerwahl(List<Player> players, string side, int bg, bool tor)
    {
        var b = new Buttons();
        // Der Knopf meldet seinen Platz, nicht die Nummer — welcher Spieler auf
        // Platz 7 steht, weiss TcuConsole aus dem aktuellen Kader.
        foreach (var (row, col, index, _, label) in PlayerSlots(players, tor))
            b[row, col] = PlayerBtn(label, bg, [$"TcuUi=player|{side}|#{index}"]);

        b[BackRow, BackCol] = BackBtn();
        b[3, 0] = PanicBtn(navigateHome: true);

        // Kopfzeile: zeigt, wofür der nächste Spielerdruck gilt — Tor, Spieler,
        // Best Player oder Strafe. Den Text schreibt TcuConsole hinein, sobald
        // der Modus scharf gestellt ist; die Seite dient allen vier Fällen.
        // Bewusst ohne Bild, sonst liesse sich der Text nicht ändern.
        b[HeadRow, HeadCol] = LabelBtn(tor ? "Tor" : "Spieler", bg);

        // Das Eigentor gehört nur in den Tor-Ablauf — bei Strafe, Namens- und
        // Best-Player-Wahl wäre es sinnlos. Deshalb gibt es die Spielerwahl
        // zweimal: einmal mit diesem Knopf, einmal ohne.
        if (tor)
            b[3, OwnGoalCol] = MultiBtn("Eigentor", bg, [$"TcuUi=owngoal|{side}"], navPage: 1,
                                        icon: TcuIcons.Tor, mode: $"owngoal_{side}");

        b[TalkRow, TalkCol] = TalkBtn();
        return b;
    }

    // ── Seiten 4–5: Starting 6 ──────────────────────────────────────────────
    //
    // Die sechs Knöpfe zeigen nur die Namen — sie haben bewusst keine Funktion.
    // TCunihockey führt die Starting Six selbst: der Modus wird auf Seite 1
    // gesetzt, "Weiter" blendet beim ersten Druck ein (TCunihockey zeigt dann
    // von selbst den ersten Spieler) und blättert danach je einen weiter.
    static Buttons P4_Starting6(List<Player> starting6, string side, int bg)
    {
        var b = new Buttons();
        foreach (var (col, _, label) in Starting6Slots(starting6))
            b[Starting6Row, col] = LabelBtn(label, bg);
        b[0, 7] = UdpBtn("▶▶ Weiter", CPeriod, [$"TcuUi=s6next|{side}"]);
        b[BackRow, BackCol] = BackBtn();
        b[3, 0] = PanicBtn(navigateHome: true);
        b[3, 1] = LetterBtn("S"); b[3, 2] = LetterBtn("T"); b[3, 3] = LetterBtn("A");
        b[3, 4] = LetterBtn("R"); b[3, 5] = LetterBtn("T"); b[3, 6] = LetterBtn("6");
        b[TalkRow, TalkCol] = TalkBtn();
        return b;
    }

    // ── Seiten 6–7: Strafe ───────────────────────────────────────────────────
    // Strafen kennt der UDP-Befehlssatz von TCunihockey nicht. Die Dauer wird
    // deshalb über die UI-Brücke geklickt (Knopf "Strafe" der Mannschaft, dann
    // 2' / 2+2' / 10' / Match), anschliessend geht es wie bei Tor und Name zur
    // Spielerwahl — dort wählt lowerthird_{side}_player|{nr} per UDP den
    // Spieler für den eingestellten Modus.
    static Buttons P6_Strafe(string side, int bg)
    {
        int playerPage = side == "home" ? 2 : 3;
        var b = new Buttons();
        b[0, 0] = MultiBtn("2'",    bg, [$"TcuUi=prepare|penalty|{side}|2"],     navPage: playerPage, icon: TcuIcons.Strafe, mode: $"penalty2_{side}");
        b[0, 1] = MultiBtn("2'+2'", bg, [$"TcuUi=prepare|penalty|{side}|22"],    navPage: playerPage, icon: TcuIcons.Strafe, mode: $"penalty22_{side}");
        b[0, 2] = MultiBtn("10'",   bg, [$"TcuUi=prepare|penalty|{side}|10"],    navPage: playerPage, icon: TcuIcons.Strafe, mode: $"penalty10_{side}");
        b[0, 3] = MultiBtn("Match", bg, [$"TcuUi=prepare|penalty|{side}|match"], navPage: playerPage, icon: TcuIcons.Strafe, mode: $"penaltyM_{side}");
        b[BackRow, BackCol] = BackBtn();
        b[3, 0] = PanicBtn(navigateHome: true);
        b[3, 1] = LetterBtn("S"); b[3, 2] = LetterBtn("T"); b[3, 3] = LetterBtn("R");
        b[3, 4] = LetterBtn("A"); b[3, 5] = LetterBtn("F"); b[3, 6] = LetterBtn("E");
        b[TalkRow, TalkCol] = TalkBtn();
        return b;
    }

    // ── Seite 8: Meldung ────────────────────────────────────────────────────
    //
    // Links die drei Störungsmeldungen, rechts die Zuschauerzahl. TCunihockey
    // hat für die Zahl weder Zähler noch eigene Einblendung — sie wird als
    // freie Mitteilung erfasst. TcuConsole führt den Zähler und schreibt
    // "Zuschauerzahl: n" ins Feld unten links.
    //
    // Beides hat keinen UDP-Befehl und läuft deshalb über die UI-Brücke.
    static Buttons P8_Meldung()
    {
        var b = new Buttons();

        // Die drei Störungsmeldungen laufen NICHT über die Shortcut-Knöpfe von
        // TCunihockey — dort ist nur die technische Störung hinterlegt.
        // Stattdessen schreibt TcuConsole den Text ins freie Feld und blendet
        // ihn ein. Über die Leitung geht nur die Kennung: das Companion-Modul
        // sendet Latin-1, die Umlaute in "Störung" und "Lösung" kämen
        // zerbrochen an. TcuConsole setzt den Text als Unicode.
        //
        // Alle drei führen zurück zur Startseite: die Meldung ist mit einem
        // Druck erledigt, danach will man wieder an die Einblendungen.
        b[0, 0] = MultiBtn("Technik", CProb, ["TcuUi=fault|technik"], navPage: 1, icon: TcuIcons.Meldung, mode: "fault_technik");
        b[0, 1] = MultiBtn("Video",   CProb, ["TcuUi=fault|bild"],    navPage: 1, icon: TcuIcons.Meldung, mode: "fault_bild");
        b[0, 2] = MultiBtn("Audio",   CProb, ["TcuUi=fault|ton"],     navPage: 1, icon: TcuIcons.Meldung, mode: "fault_ton");

        // Die Kurztext-Knöpfe aus der System-Konfig sind weggefallen: belegt war
        // dort nur der technische Störungstext, und den gibt es jetzt als
        // eigenen Knopf.

        b[0, 5] = UdpBtn("+100", CCntP, ["TcuUi=spectators|100"]);
        b[0, 6] = UdpBtn("+10",  CCntP, ["TcuUi=spectators|10"]);
        b[0, 7] = UdpBtn("+1",   CCntP, ["TcuUi=spectators|1"]);
        // Anzeigefeld links von "Anzeigen": TcuConsole schreibt den Zählerstand
        // hinein. Ohne Bild, sonst liesse sich der Text nicht mehr ändern.
        b[SpectatorsRow, SpectatorsCol] = LabelBtn("Zuschauer\n0", CNeutral);
        // Anzeigen beendet den Vorgang — die Zahl ist erfasst und auf Sendung,
        // also zurück zur Startseite. Die Zählknöpfe bleiben bewusst hier:
        // dort wird mehrfach hintereinander gedrückt.
        b[1, 7] = MultiBtn("👁 Anzeigen", CDisp, ["TcuUi=spectators|show"], navPage: 1,
                           mode: "spectators");
        b[2, 5] = UdpBtn("-1",   CCntM, ["TcuUi=spectators|-1"]);
        b[2, 6] = UdpBtn("-10",  CCntM, ["TcuUi=spectators|-10"]);
        b[2, 7] = UdpBtn("-100", CCntM, ["TcuUi=spectators|-100"]);
        b[BackRow, BackCol] = BackBtn();
        b[3, 0] = PanicBtn(navigateHome: true);
        b[3, 1] = LetterBtn("M"); b[3, 2] = LetterBtn("E"); b[3, 3] = LetterBtn("L");
        b[3, 4] = LetterBtn("D"); b[3, 5] = LetterBtn("U"); b[3, 6] = LetterBtn("NG");
        b[TalkRow, TalkCol] = TalkBtn();
        return b;
    }

    // ── Seite 9: Drittelwahl ────────────────────────────────────────────────
    //
    // Die frühere Matchuhr-Seite ist weg: von der Uhr wird genau ein Knopf
    // gebraucht — Start/Stop —, und der steht auf der Startseite. Eine eigene
    // Seite für Sekundenkorrekturen hat sich nie gerechnet. Hier bleibt, was
    // TCunihockey nicht selbst weiss — das Drittel.
    //
    // Jede Wahl führt zurück zur Startseite: das Drittel wird einmal pro
    // Spielabschnitt gesetzt, danach will man wieder an die Einblendungen.
    static Buttons P9_Drittel()
    {
        var b = new Buttons();
        b[0, 0] = MultiBtn("1. Drittel", CPeriod, ["TcuController=scoreboard_period|1"], navPage: 1, icon: TcuIcons.Drittel);
        b[0, 1] = MultiBtn("2. Drittel", CPeriod, ["TcuController=scoreboard_period|2"], navPage: 1, icon: TcuIcons.Drittel);
        b[0, 2] = MultiBtn("3. Drittel", CPeriod, ["TcuController=scoreboard_period|3"], navPage: 1, icon: TcuIcons.Drittel);
        b[0, 4] = MultiBtn("Overtime",   CPeriod, ["TcuController=scoreboard_period|O"], navPage: 1, icon: TcuIcons.Drittel);
        b[0, 5] = MultiBtn("Penalty",    CPeriod, ["TcuController=scoreboard_period|P"], navPage: 1, icon: TcuIcons.Drittel);
        b[1, 5] = UdpBtn("🥅 Penaltys\nzurücksetzen", CNeutral, ["TcuController=scoreboard_penaltyshots_reset"]);
        b[BackRow, BackCol] = BackBtn();
        b[3, 0] = PanicBtn(navigateHome: true);
        b[3, 1] = LetterBtn("D"); b[3, 2] = LetterBtn("R"); b[3, 3] = LetterBtn("I");
        b[3, 4] = LetterBtn("T"); b[3, 5] = LetterBtn("T"); b[3, 6] = LetterBtn("EL");
        b[TalkRow, TalkCol] = TalkBtn();
        return b;
    }

    // ── Seite zusammenbauen ──────────────────────────────────────────────────
    static JsonObject Page(string name, Buttons controls)
    {
        var ctrlNode = new JsonObject();
        foreach (var ((row, col), btn) in controls)
        {
            var rk = row.ToString();
            if (!ctrlNode.ContainsKey(rk)) ctrlNode[rk] = new JsonObject();
            ((JsonObject)ctrlNode[rk]!)[col.ToString()] = btn;
        }
        return new JsonObject
        {
            ["id"]       = PgId(),
            ["name"]     = name,
            ["controls"] = ctrlNode,
            ["gridSize"] = new JsonObject
            {
                ["minColumn"] = 0, ["maxColumn"] = 7,
                ["minRow"]    = 0, ["maxRow"]    = 3,
            },
        };
    }

    // ── Button-Fabrikmethoden ────────────────────────────────────────────────

    // Reine Navigation
    static JsonObject NavBtn(string text, int bg, int targetPage, string? icon = null) =>
        MakeButton(text, bg, Fg(bg), [NavAction(targetPage)], icon);

    // Nur Label, keine Aktion
    static JsonObject LabelBtn(string text, int bg, string? icon = null) =>
        MakeButton(text, bg, Fg(bg), [], icon);

    // Buchstaben-Button (untere Leiste)
    static JsonObject LetterBtn(string letter) =>
        MakeButton(letter, CLetter, CWhite, []);

    // Staffelung der Befehle eines Knopfes.
    //
    // 50 ms reichen zwischen zwei UDP-Befehlen an TCunihockey. Nach einem
    // TcuUi-Befehl nicht: der Klick wird in TCunihockey erst zugestellt und
    // dann auf dessen UI-Thread abgearbeitet — ein sofort folgendes
    // lowerthird_show würde die Einblendung senden, bevor sie steht.
    const int StepDelayUdp = 50;
    const int StepDelayUi  = 300;

    static List<JsonObject> Steps(string[] cmds)
    {
        var actions = new List<JsonObject>(cmds.Length);
        var delay   = 0;
        foreach (var c in cmds)
        {
            actions.Add(UdpAction(c, delay));
            delay += c.StartsWith("TcuUi=", StringComparison.Ordinal) ? StepDelayUi : StepDelayUdp;
        }
        return actions;
    }

    // UDP-Befehle, keine Navigation.
    // mode: Name für das optische Feedback. Gesetzt wird die Variable ZUERST,
    // damit der Knopf sofort aufleuchtet und nicht erst nach der letzten
    // Verzögerung der Befehlskette.
    static JsonObject UdpBtn(string text, int bg, string[] cmds,
                             string? icon = null, string? mode = null)
    {
        var actions = new List<JsonObject>();
        if (mode is not null) actions.Add(ModeAction(mode));
        actions.AddRange(Steps(cmds));
        return MakeButton(text, bg, Fg(bg), [.. actions], icon,
                          mode is null ? null : ModeFeedback(mode, CActive));
    }

    // UDP-Befehle + Navigation danach (z.B. Tor: sende Befehl, dann zu Spielerwahl)
    static JsonObject MultiBtn(string text, int bg, string[] cmds, int navPage,
                               string? icon = null, string? mode = null)
    {
        var actions = new List<JsonObject>();
        if (mode is not null) actions.Add(ModeAction(mode));
        actions.AddRange(Steps(cmds));
        actions.Add(NavAction(navPage));
        return MakeButton(text, bg, Fg(bg), [.. actions], icon,
                          mode is null ? null : ModeFeedback(mode, CActive));
    }

    // Spieler-Button: UDP-Befehle + ggf. zurück zu Seite 1
    static JsonObject PlayerBtn(string text, int bg, string[] cmds, int? backPage = 1)
    {
        var actions = Steps(cmds);
        if (backPage.HasValue) actions.Add(NavAction(backPage.Value));
        return MakeButton(text, bg, Fg(bg), [.. actions]);
    }

    // Panic: Hide-Befehle + optional Navigation zu Seite 1
    static JsonObject PanicBtn(bool navigateHome)
    {
        // TCunihockey hat zwar einen Panic-Knopf, aber keinen Panic-UDP-Befehl.
        // Die drei Hide-Befehle bewirken dasselbe und kommen ohne die UI-Brücke
        // aus — der Notaus soll auch dann greifen, wenn TcuConsole steht.
        string[] cmds = [
            "TcuController=lowerthird_hide",
            "TcuController=scoreboard_hide",
            "TcuController=sponsor_hide",
        ];
        // Notaus löscht auch das Feedback — nach dem Ausblenden ist kein Modus
        // mehr gewählt, und ein weiterleuchtender Knopf wäre eine Falschaussage.
        var actions = new List<JsonObject> { ModeAction("") };
        actions.AddRange(Steps(cmds));
        if (navigateHome) actions.Add(NavAction(1));
        return MakeButton("🚨 PANIC", CPanic, CWhite, [.. actions]);
    }

    // ── Lesbarkeit ───────────────────────────────────────────────────────────
    // Schriftfarbe aus der Hintergrundhelligkeit ableiten. Die Icons sind
    // dunkel umrandete Zeichnungen auf transparentem Grund und brauchen helle
    // Flächen; darauf wäre weisser Text unlesbar. Gewichtung nach
    // Wahrnehmungshelligkeit (Rec. 601), nicht als einfacher Mittelwert —
    // sonst gilt Blau als zu hell und Grün als zu dunkel.
    static int Fg(int bg)
    {
        int r = (bg >> 16) & 0xFF, g = (bg >> 8) & 0xFF, bl = bg & 0xFF;
        var luma = (0.299 * r + 0.587 * g + 0.114 * bl) / 255.0;
        return luma > 0.55 ? CDark : CWhite;
    }

    // ── Zustandsknopf ────────────────────────────────────────────────────────
    // Ein Knopf, dessen Farbe den tatsächlichen Zustand in TCunihockey zeigt:
    // grün wenn aus, rot wenn an. Den Wert schreibt TcuConsole in die Variable,
    // der Knopf trägt nur das Feedback darauf.
    //
    // Der Vorgänger war ein Zwei-Schritt-Umschalter, der bei jedem Druck selbst
    // zwischen "1" und "0" weiterzählte. Der stimmte nur, solange ausschliesslich
    // über diesen einen Knopf geschaltet wurde — nach jeder Bedienung am
    // TCunihockey-Fenster, über einen anderen Knopf oder über PANIC zeigte er
    // das Gegenteil und schaltete beim nächsten Druck falsch herum.
    //
    // Beide Farben sind dunkel gewählt, damit die ins Bild gerechnete
    // Beschriftung in beiden Zuständen weiss bleiben kann — sie wird beim
    // Erzeugen der Config gesetzt und kann zur Laufzeit nicht mitwechseln.
    static JsonObject StateBtn(string text, string variable, string[] cmds, string? icon = null)
        => MakeButton(text, CStateOff, CWhite, [.. Steps(cmds)], icon,
                      VarFeedback(variable, "1", CStateOn));

    // ── Spieluhr ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Startet und hält die Spieluhr an — ein Knopf für beides.
    ///
    /// Hier ist ein Umschalter unbedenklich, anders als beim Einblender:
    /// scoreboard_togglestartstop schaltet den tatsächlichen Lauf der Uhr um,
    /// es führt kein Nebenzähler mit, der aus dem Tritt geraten könnte.
    ///
    /// Der Befehl geht direkt an TCunihockey (7001), nicht über die UI-Brücke.
    /// Damit funktioniert der Knopf auch dann noch, wenn TcuConsole steht —
    /// dann fehlt nur die Farbe, nicht die Funktion. Genau wie Drittel und
    /// PANIC, und aus demselben Grund: an der Uhr darf nichts hängenbleiben.
    ///
    /// Die Farbe kommt aus dem, was wirklich passiert: grün, solange die Uhr
    /// tickt, rot sobald sie steht. Das ist die Umkehrung von Einblender und
    /// Werbung, wo rot "auf Sendung" heisst — bei der Uhr ist der stehende
    /// Zustand der, den man mitten im Spiel sofort sehen will.
    ///
    /// Bewusst ohne Bild: es gibt kein Uhr-Motiv im Satz, und die Aussage
    /// trägt hier ohnehin die Farbe.
    ///
    /// Vorangestelltes scoreboard_show: die Sekundenkorrektur nebenan blendet
    /// die Anzeige aus, damit das Zurückstellen nicht auf Sendung geht. Ohne
    /// das Einblenden hier bliebe sie nach der Korrektur verschwunden, und das
    /// fiele erst auf, wenn das Spiel längst wieder läuft. Der Befehl trifft
    /// auch den Normalfall, in dem die Anzeige bereits steht — dort ändert er
    /// nichts.
    /// </summary>
    static JsonObject ClockBtn() =>
        MakeButton("⏱ Uhr\nStart / Stop", CStateOn, CWhite,
                   [.. Steps([
                       "TcuController=scoreboard_show",
                       "TcuController=scoreboard_togglestartstop",
                   ])],
                   icon: null,
                   feedbacks: VarFeedback(ClockRunningVariable, "1", CStateOff));

    /// <summary>
    /// Stellt die Spieluhr um eine Sekunde zurück — beliebig oft nacheinander,
    /// jeder Druck eine Sekunde. Für den Fall, dass die Uhr zu spät gestoppt
    /// wurde.
    ///
    /// Zuerst wird die Anzeige ausgeblendet: eine rückwärts springende Uhr auf
    /// Sendung sieht nach Panne aus. scoreboard_hide ist beim zweiten und
    /// dritten Druck wirkungslos, stört also nicht. Eingeblendet wird wieder
    /// mit dem Start-Knopf nebenan.
    ///
    /// Zu beachten: TCunihockey kennt kein Ausblenden nur für die Uhr —
    /// scoreboard_hide nimmt die ganze Anzeige weg, also auch Stand und
    /// Drittel. Für die paar Sekunden einer Korrektur ist das der bessere
    /// Handel als eine sichtbar zurückspringende Uhr.
    /// </summary>
    static JsonObject ClockBackBtn() =>
        MakeButton("◀ Uhr −1 s\n(aus)", CTimer, Fg(CTimer),
                   [.. Steps([
                       "TcuController=scoreboard_hide",
                       "TcuController=scoreboard_secmin",
                   ])]);

    // ── Basis-Button ─────────────────────────────────────────────────────────
    static JsonObject MakeButton(string text, int bgcolor, int color,
                                 JsonObject[] actions, string? icon = null,
                                 JsonArray? feedbacks = null)
    {
        var arr = new JsonArray();
        foreach (var a in actions) arr.Add(a);

        // Mit Icon wandert die Beschriftung INS Bild (unteres Drittel,
        // einzeilig) und der Knopf selbst bleibt textlos. Companion kann die
        // Textfläche nicht begrenzen: size "auto" breitet die Schrift über die
        // ganze Taste aus und bricht sie um, wodurch sie das Motiv überdeckte.
        var png = icon is null ? null : TcuIcons.Get(icon, text, color);
        var buttonText = png is null ? text : "";

        return new JsonObject
        {
            ["type"]      = "button",
            ["options"]   = new JsonObject
            {
                ["relativeDelay"]    = false,
                ["rotaryActions"]    = false,
                ["stepAutoProgress"] = true,
            },
            ["style"]     = new JsonObject
            {
                ["text"]         = buttonText,
                ["size"]         = "auto",
                ["color"]        = color,
                ["bgcolor"]      = bgcolor,
                ["alignment"]    = "center:center",
                ["pngalignment"] = "center:center",
                // Das Bild MUSS in png64 stehen. "png" ist ein Altfeld, das
                // Companion beim Import nicht mehr ausliest (der Konverter
                // greift auf style.png64 zu) — dort abgelegte Bilder
                // verschwinden stillschweigend.
                ["png"]          = null,
                ["png64"]        = png,
                ["show_topbar"]  = "default",
            },
            ["feedbacks"] = feedbacks ?? new JsonArray(),
            ["steps"]     = new JsonObject
            {
                // Tastendruck gehört in "down". Ein action_sets-Eintrag "0"
                // wird von Companion beim Import ignoriert — das war der Grund,
                // warum die Buttons zwar mit Text und Farbe ankamen, aber ohne
                // jede Aktion (action_sets: {"0":[], "down":[], "up":[]}).
                ["0"] = new JsonObject
                {
                    ["action_sets"] = new JsonObject
                    {
                        ["down"]         = arr,
                        ["up"]           = new JsonArray(),
                        ["rotate_left"]  = new JsonArray(),
                        ["rotate_right"] = new JsonArray(),
                    },
                    ["options"]     = new JsonObject { ["runWhileHeld"] = new JsonArray() },
                },
            },
        };
    }

    // ── Actions ──────────────────────────────────────────────────────────────
    static JsonObject UdpAction(string command, int delay = 0) => new()
    {
        ["id"]       = Id(),
        // Präfix bestimmt den Empfänger: TcuUi= geht an die Brücke in
        // TcuConsole (7002), alles andere direkt an TCunihockey (7001).
        ["instance"] = command.StartsWith("TcuUi=", StringComparison.Ordinal) ? "tcuui" : "tcu",
        ["action"]   = "send",
        // id_end = "" (None): TcuUdp.Send schickt den Befehl ebenfalls ohne
        // Zeilenende, das ist die einzige Variante, für die es hier Evidenz gibt.
        ["options"]  = new JsonObject { ["id_send"] = command, ["id_end"] = "" },
        ["delay"]    = delay,
        ["disabled"] = false,
    };

    // Seitenwechsel. Die Internal-Action heisst "set_page" — ein
    // "jump_to_page_val" kennt Companion 5 nicht (kommt im gesamten
    // Companion-Code kein einziges Mal vor), damit wäre auch jede
    // Navigation wirkungslos geblieben.
    static JsonObject NavAction(int page) => new()
    {
        ["id"]       = Id(),
        ["instance"] = "internal",
        ["action"]   = "set_page",
        ["options"]  = new JsonObject
        {
            ["controller_from_variable"] = false,
            ["controller"]               = "self",
            ["controller_variable"]      = "",
            ["page_from_variable"]       = false,
            ["page"]                     = page,
            ["page_variable"]            = "",
        },
        ["delay"]    = 150,
        ["disabled"] = false,
    };

    // ── Hilfsmethoden ────────────────────────────────────────────────────────
    static string Trunc(string s, int max) => s.Length <= max ? s : s[..max];

    class Buttons : Dictionary<(int row, int col), JsonObject>
    {
        public JsonObject this[int row, int col]
        {
            get => this[(row, col)];
            set => this[(row, col)] = value;
        }
    }
}
