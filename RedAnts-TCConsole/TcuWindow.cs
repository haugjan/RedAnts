using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace TcuConsole;

/// <summary>
/// Findet das Bedienfenster von TCunihockey.
///
/// Bewusst NICHT über Process.MainWindowHandle: das liefert das erste sichtbare
/// Fenster des Prozesses, und das ist jedes Meldungsfenster, das TCunihockey
/// aufmacht. Gemessen am 21.08.2026, während ein Fehlerdialog offen stand:
///
///   0x0009050A  kinder=  3   312x166    #32770                   ← MainWindowHandle
///   0x000400FE  kinder=212   1600x900   WindowsForms10.Window... ← die Bedienoberfläche
///
/// Solange so ein Dialog offen ist, würden Klicks und Abfragen ins leere
/// Fenster gehen und still nichts tun. Deshalb wird hier das WindowsForms-
/// Fenster mit den meisten Kind-Fenstern gesucht — die Bedienoberfläche hat
/// über 200, jedes Meldungsfenster eine Handvoll.
/// </summary>
public static class TcuWindow
{
    public const string ProcessName = "TCunihockey";

    // Ab so vielen Kind-Fenstern gilt ein Fenster als die Bedienoberfläche.
    // Gemessen: 212 in v6.2.12, 3 im Meldungsfenster.
    const int MinChildren = 50;

    const string FormsClassPrefix = "WindowsForms10.Window.8.app";

    static nint _cached;
    static int  _cachedPid = -1;

    /// <summary>Handle der Bedienoberfläche, 0 wenn TCunihockey nicht läuft.</summary>
    public static nint Handle()
    {
        // Schneller Pfad: der gemerkte Handle wird allein am Fenster geprüft,
        // ohne die Prozessliste anzufassen.
        //
        // Das ist der springende Punkt für die Laufzeit: Process
        // .GetProcessesByName durchsucht sämtliche Prozesse des Rechners und
        // kostet gemessen 4,9 ms — bei zwei Abfragen je Sekunde über ein
        // ganzes Spiel wäre das der teuerste Posten der ganzen Anwendung.
        // IsWindow und GetClassName kosten Mikrosekunden.
        //
        // Ebenfalls bewusst OHNE erneute Prüfung der Kinderzahl: ein später
        // geöffnetes Meldungsfenster ändert an diesem Handle nichts — genau
        // darum geht es ja.
        if (_cached != 0 && IsWindow(_cached) && IsFormsWindow(_cached))
            return _cached;

        var pid = Pid();
        if (pid < 0) { _cached = 0; _cachedPid = -1; return 0; }

        _cached    = Search(pid);
        _cachedPid = _cached == 0 ? -1 : pid;
        return _cached;
    }

    static bool IsFormsWindow(nint hwnd)
    {
        var cls = new StringBuilder(256);
        GetClassName(hwnd, cls, cls.Capacity);
        return cls.ToString().StartsWith(FormsClassPrefix, StringComparison.Ordinal);
    }

    /// <summary>Prozess-Id von TCunihockey, -1 wenn es nicht läuft.
    /// Die Process-Objekte werden freigegeben: bei zwei Abfragen je Sekunde
    /// über ein ganzes Spiel wären das sonst zehntausende offene Handles.</summary>
    static int Pid()
    {
        var procs = Process.GetProcessesByName(ProcessName);
        try { return procs.Length == 0 ? -1 : procs[0].Id; }
        finally { foreach (var p in procs) p.Dispose(); }
    }

    static nint Search(int pid)
    {
        nint best = 0;
        var  bestCount = -1;

        // Der Delegate muss bis zum Ende der Aufzählung am Leben bleiben.
        EnumProc scan = (h, _) =>
        {
            GetWindowThreadProcessId(h, out var owner);
            if (owner != pid || !IsWindowVisible(h)) return true;

            var cls = new StringBuilder(256);
            GetClassName(h, cls, cls.Capacity);
            if (!cls.ToString().StartsWith(FormsClassPrefix, StringComparison.Ordinal)) return true;

            var n = ChildCount(h);
            if (n > bestCount) { bestCount = n; best = h; }
            return true;
        };
        EnumWindows(scan, 0);
        GC.KeepAlive(scan);

        return bestCount >= MinChildren ? best : 0;
    }

    static int ChildCount(nint hwnd)
    {
        var n = 0;
        EnumProc count = (_, _) => { n++; return true; };
        EnumChildWindows(hwnd, count, 0);
        GC.KeepAlive(count);
        return n;
    }

    /// <summary>Alle offenen Meldungsfenster (#32770) des Prozesses samt Text.
    /// Ein offener Dialog blockiert TCunihockey vollständig — Befehle werden
    /// dann zwar angenommen, aber nicht abgearbeitet. Das muss auffallen.</summary>
    public static List<string> Dialogs()
    {
        var found = new List<string>();

        // Prozess-Id über das bereits gefundene Bedienfenster statt über die
        // Prozessliste — siehe Handle(): die Liste kostet 4,9 ms, das Fenster
        // Mikrosekunden.
        var main = Handle();
        if (main == 0) return found;
        GetWindowThreadProcessId(main, out var pid);

        EnumProc scan = (h, _) =>
        {
            GetWindowThreadProcessId(h, out var owner);
            if (owner != pid || !IsWindowVisible(h)) return true;

            var cls = new StringBuilder(64);
            GetClassName(h, cls, cls.Capacity);
            if (cls.ToString() != "#32770") return true;

            // Der Meldungstext steht im längsten STATIC des Dialogs.
            var texts = new List<string>();
            EnumProc kids = (k, _) =>
            {
                var kc = new StringBuilder(64); GetClassName(k, kc, kc.Capacity);
                if (kc.ToString() == "Static")
                {
                    var t = new StringBuilder(1024); GetWindowTextW(k, t, t.Capacity);
                    texts.Add(t.ToString());
                }
                return true;
            };
            EnumChildWindows(h, kids, 0);
            GC.KeepAlive(kids);

            var msg = texts.OrderByDescending(t => t.Length).FirstOrDefault() ?? "";
            found.Add(msg.Replace("\r", "").Replace("\n", " ").Trim());
            return true;
        };
        EnumWindows(scan, 0);
        GC.KeepAlive(scan);

        return found;
    }

    // ── Win32 ────────────────────────────────────────────────────────────────
    delegate bool EnumProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, nint lParam);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(nint hwnd, EnumProc cb, nint lParam);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(nint hwnd, out int pid);
    [DllImport("user32.dll")] static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(nint hwnd, StringBuilder buf, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowTextW(nint hwnd, StringBuilder buf, int max);
}
