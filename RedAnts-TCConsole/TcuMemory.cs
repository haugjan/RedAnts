using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime;

namespace TcuConsole;

/// <summary>
/// Liest den Zustand direkt aus dem verwalteten Heap von TCunihockey.exe
/// (.NET Framework 4.8, WinForms, VB.NET). Rein lesend, der Prozess wird
/// nicht angehalten.
///
/// Warum überhaupt: der Kader steht in keiner Stelle der Oberfläche, die
/// UI Automation sieht — die Spielerliste existiert nur als
/// ContextMenuStrip, dessen Einträge erst beim Öffnen im Baum erscheinen.
/// Im Speicher liegt sie dagegen deterministisch benannt vor:
///
///   TCunihockey.FrmMain
///     lstHomePlayers : List&lt;FrmMain+PlayerInfo&gt;
///     lstAwayPlayers : List&lt;FrmMain+PlayerInfo&gt;
///       PlayerInfo._Number : String   ("00" bleibt dadurch erhalten)
///       PlayerInfo._Name   : String
///     strScoreHomeTemp / strScoreAwayTemp : String
///     strNumbersHomeStarting6 / strNumbersAwayStarting6 : String
///     intCountStarting6 : Int32
///     isPlayerSelectionHomeAllowed / ...AwayAllowed : Boolean
///     intControlExternalMode : Int32
///     LblHome, LblAway, LblScoreboardHome, LblScoreboardAway : Label
///
/// Die Mannschaftsnamen kommen über die Labels: jedes WinForms-Steuerelement
/// hält sein Fenster in Control.window (NativeWindow, Feld "handle"), und
/// dessen Fenstertext ist der Name. UI Automation findet genau diese Labels bei
/// minimiertem TCunihockey nicht — der Heap kennt sie immer.
/// </summary>
public sealed class TcuMemory(TcuLogger logger)
{
    public const string ProcessName = "TCunihockey";

    public sealed class Snapshot
    {
        public List<Player> HomePlayers { get; init; } = [];
        public List<Player> AwayPlayers { get; init; } = [];
        public string HomeTeam { get; init; } = "";
        public string AwayTeam { get; init; } = "";
        public string HomeTeamShort { get; init; } = "";
        public string AwayTeamShort { get; init; } = "";
        public string ScoreHome { get; init; } = "";
        public string ScoreAway { get; init; } = "";
        public string Starting6Home { get; init; } = "";
        public string Starting6Away { get; init; } = "";
        public int    Starting6Count { get; init; }
        public bool   PlayerSelectionHome { get; init; }
        public bool   PlayerSelectionAway { get; init; }
        public int    ExternalMode { get; init; }
        public bool   ControlApiEnabled { get; init; }
        public int    ControlApiPort { get; init; }
    }

    /// <summary>Liest einen Schnappschuss. null, wenn TCunihockey nicht läuft
    /// oder der Heap nicht lesbar ist.</summary>
    public Snapshot? TryRead()
    {
        Snapshot? result = null;
        WithMainForm(frm => result = new Snapshot
        {
            HomePlayers         = ReadPlayerList(frm, "lstHomePlayers"),
            AwayPlayers         = ReadPlayerList(frm, "lstAwayPlayers"),
            HomeTeam            = LabelText(frm, "LblHome"),
            AwayTeam            = LabelText(frm, "LblAway"),
            HomeTeamShort       = LabelText(frm, "LblScoreboardHome"),
            AwayTeamShort       = LabelText(frm, "LblScoreboardAway"),
            ScoreHome           = Str(frm, "strScoreHomeTemp"),
            ScoreAway           = Str(frm, "strScoreAwayTemp"),
            Starting6Home       = Str(frm, "strNumbersHomeStarting6"),
            Starting6Away       = Str(frm, "strNumbersAwayStarting6"),
            Starting6Count      = Int(frm, "intCountStarting6"),
            PlayerSelectionHome = Bool(frm, "isPlayerSelectionHomeAllowed"),
            PlayerSelectionAway = Bool(frm, "isPlayerSelectionAwayAllowed"),
            ExternalMode        = Int(frm, "intControlExternalMode"),
            ControlApiEnabled   = Bool(frm, "boolControlApiEnabled"),
            ControlApiPort      = Int(frm, "udpControlApiPort"),
        });
        return result;
    }

    /// <summary>
    /// Fenster-Handles von Steuerelementen des Hauptfensters, über den Heap.
    /// Für das, was UI Automation nicht findet — bei minimiertem TCunihockey
    /// etwa die Mannschaftsnamen.
    /// </summary>
    public Dictionary<string, nint> ControlHandles(IEnumerable<string> names)
    {
        var result = new Dictionary<string, nint>(StringComparer.Ordinal);
        WithMainForm(frm =>
        {
            foreach (var name in names)
            {
                var h = ControlHandle(frm, name);
                if (h != 0) result[name] = h;
            }
        });
        return result;
    }

    bool WithMainForm(Action<ClrObject> use)
    {
        var proc = Process.GetProcessesByName(ProcessName).FirstOrDefault();
        if (proc is null)
        {
            logger.Log($"{ProcessName} läuft nicht — Speicher nicht lesbar", LogLevel.Warning);
            return false;
        }

        try
        {
            // suspend:false — der Prozess läuft weiter, wir lesen nur mit
            using var target = DataTarget.AttachToProcess(proc.Id, suspend: false);
            var clr = target.ClrVersions.FirstOrDefault();
            if (clr is null) { logger.Log("Keine CLR im Zielprozess gefunden", LogLevel.Error); return false; }

            using var runtime = clr.CreateRuntime();
            var heap = runtime.Heap;
            if (!heap.CanWalkHeap) { logger.Log("Heap nicht begehbar", LogLevel.Warning); return false; }

            foreach (var o in heap.EnumerateObjects())
            {
                if (!o.IsValid || o.Type?.Name != "TCunihockey.FrmMain") continue;
                use(o);
                return true;
            }

            logger.Log("FrmMain nicht im Heap gefunden", LogLevel.Warning);
            return false;
        }
        catch (Exception ex)
        {
            logger.Log($"Speicherzugriff fehlgeschlagen: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    // ── List<PlayerInfo> auslesen ────────────────────────────────────────────
    // List<T> hält die Elemente in _items (T[]) und die Anzahl in _size;
    // _items ist regelmässig länger als _size, deshalb zwingend über _size.
    static List<Player> ReadPlayerList(ClrObject frm, string fieldName)
    {
        var result = new List<Player>();
        try
        {
            var list = frm.ReadObjectField(fieldName);
            if (list.IsNull) return result;

            var size  = list.ReadField<int>("_size");
            var items = list.ReadObjectField("_items");
            if (items.IsNull || !items.IsArray) return result;

            var arr = items.AsArray();
            var count = Math.Min(size, arr.Length);

            for (int i = 0; i < count; i++)
            {
                var pi     = arr.GetStructValue(i);
                var number = (pi.ReadStringField("_Number") ?? "").Trim();
                var name   = (pi.ReadStringField("_Name")   ?? "").Trim();
                if (number.Length == 0 || name.Length == 0) continue;

                result.Add(new Player
                {
                    // NrText bewahrt "00"; Nr dient nur zum Sortieren
                    NrText = number,
                    Nr     = int.TryParse(number, out var n) ? n : 0,
                    Name   = name,
                });
            }
        }
        catch { /* Teilergebnis ist besser als keins */ }
        return result;
    }

    // ── Steuerelemente ───────────────────────────────────────────────────────
    // VB.NET legt das Feld eines Steuerelements je nach Compiler unter dem
    // Namen selbst, als "_Name" oder als "<Name>k__BackingField" an — der
    // Dekompilierer zeigt LblHome als Eigenschaft mit verstecktem Feld.
    static nint ControlHandle(ClrObject frm, string name)
    {
        foreach (var field in new[] { name, "_" + name, $"<{name}>k__BackingField" })
        {
            try
            {
                if (frm.Type?.GetFieldByName(field) is null) continue;
                var control = frm.ReadObjectField(field);
                if (control.IsNull) continue;
                var window = control.ReadObjectField("window");
                if (window.IsNull) continue;
                var handle = window.ReadField<nint>("handle");
                if (handle != 0) return handle;
            }
            catch { }
        }
        return 0;
    }

    static string LabelText(ClrObject frm, string name)
    {
        var h = ControlHandle(frm, name);
        if (h == 0) return "";
        var sb = new StringBuilder(256);
        GetWindowTextW(h, sb, sb.Capacity);
        return sb.ToString().Trim();
    }

    static string Str(ClrObject o, string f) { try { return o.ReadStringField(f) ?? ""; } catch { return ""; } }
    static int    Int(ClrObject o, string f) { try { return o.ReadField<int>(f);      } catch { return 0;  } }
    static bool   Bool(ClrObject o, string f){ try { return o.ReadField<bool>(f);     } catch { return false; } }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowTextW(nint hwnd, StringBuilder text, int max);
}
