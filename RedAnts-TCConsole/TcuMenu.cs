using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace TcuConsole;

/// <summary>
/// Öffnet das Spieler-Kontextmenü von TCunihockey und wählt einen Eintrag.
///
/// Gebraucht wird das für genau eine Sache: das Eigentor. Es hat keinen
/// UDP-Befehl und keinen Knopf im Fenster — es steht ausschliesslich als
/// letzter Eintrag im Menü, das ein Rechtsklick auf die Spielernummer öffnet.
/// (Geprüft: Gegner-Nummer bei scharfem Tor-Modus wählt trotzdem den eigenen
/// Spieler; "0", "ET" und leer fallen alle auf den Spieler "00" zurück.)
///
/// Warum echte Mauseingabe und nicht PostMessage: WinForms öffnet ein
/// ContextMenuStrip nicht auf eine zugestellte Nachricht hin. Durchprobiert
/// wurden WM_RBUTTONDOWN/UP und WM_CONTEXTMENU auf allen 14 Bedienelementen des
/// Mannschaftsbereichs — kein einziges öffnete ein Menü. Mit echtem Rechtsklick
/// erscheint es sofort.
///
/// Der Eingriff ist deshalb so kurz wie möglich gehalten: Mauszeiger und
/// Vordergrundfenster werden vorher gemerkt und hinterher zurückgesetzt, und
/// der Menüeintrag wird nicht angeklickt, sondern über UI Automation aufgerufen
/// — die Maus muss also nur einmal an eine Stelle und wieder zurück.
/// </summary>
public static class TcuMenu
{
    /// <summary>Wie lange auf das Menü gewartet wird.</summary>
    const int MenuTimeoutMs = 1500;

    /// <summary>
    /// Rechtsklick auf <paramref name="target"/>, dann den Eintrag mit diesem
    /// Namen aufrufen. Rückgabe: Meldung fürs Log, null bei Erfolg.
    /// </summary>
    public static string? Pick(nint target, string entry, TcuLogger logger)
    {
        if (target == 0) return "Kein Ziel für das Kontextmenü";

        var main = TcuWindow.Handle();
        if (main == 0) return "Bedienfenster nicht gefunden";

        var proc = Process.GetProcessesByName(TcuWindow.ProcessName).FirstOrDefault();
        if (proc is null) return "TCunihockey läuft nicht";
        var pid = proc.Id;
        proc.Dispose();

        GetWindowRect(target, out var r);
        GetCursorPos(out var mausVorher);
        var fensterVorher = GetForegroundWindow();
        var vorher        = TopLevel(pid);

        try
        {
            var x = r.L + (r.R - r.L) / 2;
            var y = r.T + (r.B - r.T) / 2;

            // Vor dem Klick prüfen, dass an dieser Stelle wirklich TCunihockey
            // liegt. Ein echter Mausklick geht an das Fenster unter dem Zeiger —
            // ist TCunihockey verdeckt, landete er in einer fremden Anwendung.
            // Lieber gar nicht klicken als irgendwohin.
            if (!Frei(x, y, main))
            {
                SetForegroundWindow(main);
                Thread.Sleep(250);
                SetCursorPos(x, y);
                Thread.Sleep(80);
                if (!Frei(x, y, main))
                    return "TCunihockey ist verdeckt — Fenster in den Vordergrund holen";
            }

            SetCursorPos(x, y);
            Thread.Sleep(60);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            Thread.Sleep(40);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

            var menu = WaitForMenu(pid, vorher);
            if (menu == 0) return "Kontextmenü ist nicht aufgegangen";

            var item = FindEntry(menu, entry);
            if (item is null)
            {
                Close();
                return $"Eintrag '{entry}' steht nicht im Menü";
            }

            item.Invoke();
            logger.LogUi($"Kontextmenü: '{entry}' gewählt");
            return null;
        }
        catch (Exception ex)
        {
            Close();
            return $"Kontextmenü fehlgeschlagen: {ex.Message}";
        }
        finally
        {
            // Immer zurückgeben, was wir uns geliehen haben — sonst steht der
            // Mauszeiger nach jedem Eigentor woanders.
            SetCursorPos(mausVorher.X, mausVorher.Y);
            if (fensterVorher != 0 && fensterVorher != main) SetForegroundWindow(fensterVorher);
        }

        static void Close()
        {
            keybd_event(VK_ESCAPE, 0, 0, 0);
            keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYUP, 0);
            Thread.Sleep(120);
        }
    }

    /// <summary>Liegt an dieser Bildschirmstelle das gesuchte Fenster —
    /// also nichts darüber?</summary>
    static bool Frei(int x, int y, nint main)
    {
        var unter = WindowFromPoint(new POINT { X = x, Y = y });
        if (unter == 0) return false;
        var wurzel = GetAncestor(unter, GA_ROOT);
        return wurzel == main || unter == main;
    }

    /// <summary>Wartet auf das neue Menüfenster des Prozesses.</summary>
    static nint WaitForMenu(int pid, HashSet<nint> vorher)
    {
        var bis = Environment.TickCount64 + MenuTimeoutMs;
        while (Environment.TickCount64 < bis)
        {
            var neu = TopLevel(pid).Except(vorher).ToList();
            if (neu.Count > 0) return neu[0];
            Thread.Sleep(40);
        }
        return 0;
    }

    static InvokePattern? FindEntry(nint menu, string name)
    {
        try
        {
            var root = AutomationElement.FromHandle(menu);
            foreach (AutomationElement el in root.FindAll(TreeScope.Descendants, Condition.TrueCondition))
            {
                if (!string.Equals(el.Current.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase)) continue;
                if (el.TryGetCurrentPattern(InvokePattern.Pattern, out var p)) return (InvokePattern)p;
            }
        }
        catch { }
        return null;
    }

    static HashSet<nint> TopLevel(int pid)
    {
        var set = new HashSet<nint>();
        EnumProc scan = (h, _) =>
        {
            GetWindowThreadProcessId(h, out var owner);
            if (owner == pid && IsWindowVisible(h)) set.Add(h);
            return true;
        };
        EnumWindows(scan, 0);
        GC.KeepAlive(scan);
        return set;
    }

    // ── Win32 ────────────────────────────────────────────────────────────────
    const uint MOUSEEVENTF_RIGHTDOWN = 0x0008, MOUSEEVENTF_RIGHTUP = 0x0010;
    const uint KEYEVENTF_KEYUP = 0x0002;
    const byte VK_ESCAPE = 0x1B;

    delegate bool EnumProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, nint lParam);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(nint hwnd, out int pid);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] static extern bool GetWindowRect(nint hwnd, out RECT r);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] static extern void mouse_event(uint flags, int dx, int dy, uint data, nint extra);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, nint extra);
    [DllImport("user32.dll")] static extern nint WindowFromPoint(POINT p);
    [DllImport("user32.dll")] static extern nint GetAncestor(nint hwnd, uint flags);

    const uint GA_ROOT = 2;

    [StructLayout(LayoutKind.Sequential)] struct RECT  { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
}
