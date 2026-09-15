using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace TcuConsole;

/// <summary>
/// Wählt einen Eintrag in einem Menü von TCunihockey — ohne Maus, ohne
/// Tastatur und ohne TCunihockey den Fokus zu geben.
///
/// Früher ging das über einen echten Rechtsklick mit anschliessendem globalem
/// Escape. Beides trifft das, was gerade vorne liegt: war TCunihockey verdeckt
/// oder minimiert, ging das Kontextmenü des Desktops auf.
///
/// TCunihockey öffnet seine Menüs auf zwei Arten, und für jede gibt es einen
/// eigenen Weg (nachgelesen im Code von TCunihockey und von WinForms, am
/// laufenden TCunihockey bei minimiertem Fenster geprüft):
///
///  * Am Element angehängt (ContextMenuStrip, beim Spielstand): WM_CONTEXTMENU
///    mit lParam = -1 öffnet es. WinForms zeigt das Menü dann mittig am Element
///    und prüft nicht, was auf dem Bildschirm liegt — es geht auch minimiert.
///
///  * Aus einem Click-Handler geöffnet (die Spielerwahl mit "Eigentor"): WinForms
///    löst Click nur aus, wenn das Element an seiner Bildschirmstelle zuoberst
///    liegt. Das Fenster wird dafür, falls nötig, kurz ohne Aktivierung nach oben
///    geholt, der Klick als Fensternachricht zugestellt und die Lage danach
///    wiederhergestellt. Der Fokus bleibt, wo er ist.
///
/// Den Eintrag löst in beiden Fällen UI Automation aus. Ein Menü, in dem der
/// Eintrag fehlt, wird gezielt an seinem eigenen Fenster geschlossen.
/// </summary>
public static class TcuMenu
{
    /// <summary>Wie lange auf das Menü gewartet wird.</summary>
    const int MenuTimeoutMs = 1500;

    /// <summary>Wie lange auf das angehobene Fenster gewartet wird.</summary>
    const int RaiseTimeoutMs = 1000;

    /// <summary>
    /// Öffnet das am Element angehängte Menü und wählt den Eintrag. Rückgabe:
    /// Meldung fürs Log, null bei Erfolg.
    /// </summary>
    public static string? PickAttached(nint target, string entry, TcuLogger logger)
    {
        if (target == 0) return "Kein Ziel für das Kontextmenü";

        var pid = ProcessId();
        if (pid == 0) return "TCunihockey läuft nicht";

        var vorher = TopLevel(pid);
        if (SendMessageTimeoutW(target, WM_CONTEXTMENU, target, -1, SMTO_ABORTIFHUNG, 1000, out _) == 0)
            return "TCunihockey reagiert nicht";

        return Choose(pid, vorher, entry, logger);
    }

    /// <summary>
    /// Klickt das Element per Fensternachricht an, wartet auf das Menü, das
    /// TCunihockey daraufhin öffnet, und wählt den Eintrag. Rückgabe: Meldung
    /// fürs Log, null bei Erfolg.
    /// </summary>
    public static string? PickClicked(nint target, string entry, TcuLogger logger)
    {
        if (target == 0) return "Kein Ziel für das Menü";

        var main = TcuWindow.Handle();
        if (main == 0) return "Bedienfenster nicht gefunden";

        var pid = ProcessId();
        if (pid == 0) return "TCunihockey läuft nicht";

        var lage = Raise(main, target);
        try
        {
            if (lage is { Ok: false })
                return "TCunihockey liess sich nicht nach oben holen";

            var vorher = TopLevel(pid);
            GetClientRect(target, out var c);
            var point = MakeLParam((c.R - c.L) / 2, (c.B - c.T) / 2);
            PostMessageW(target, WM_LBUTTONDOWN, MK_LBUTTON, point);
            PostMessageW(target, WM_LBUTTONUP, 0, point);

            return Choose(pid, vorher, entry, logger);
        }
        finally
        {
            lage?.Restore();
        }
    }

    static string? Choose(int pid, HashSet<nint> vorher, string entry, TcuLogger logger)
    {
        var menu = WaitForMenu(pid, vorher);
        if (menu == 0) return "Menü ist nicht aufgegangen";

        try
        {
            var item = FindEntry(menu, entry);
            if (item is null)
            {
                CloseMenu(menu);
                return $"Eintrag '{entry}' steht nicht im Menü";
            }

            item.Invoke();
            logger.LogUi($"Menü: '{entry}' gewählt");
            return null;
        }
        catch (Exception ex)
        {
            CloseMenu(menu);
            return $"Menü fehlgeschlagen: {ex.Message}";
        }
    }

    // ── Fenster nach oben holen, ohne es zu aktivieren ───────────────────────

    sealed class Lage(nint main, bool wasIconic, nint above)
    {
        public bool Ok { get; set; }

        public void Restore()
        {
            SetWindowPos(main, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
            if (above != 0) SetWindowPos(main, above, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
            if (wasIconic) ShowWindowAsync(main, SW_SHOWMINNOACTIVE);
        }
    }

    /// <summary>
    /// Holt TCunihockey nach oben, wenn das Element nicht schon zuoberst liegt.
    /// null heisst: nicht nötig, nichts zurückzusetzen.
    /// </summary>
    static Lage? Raise(nint main, nint target)
    {
        var wasIconic = IsIconic(main);
        if (!wasIconic && OnTop(main, target)) return null;

        var lage = new Lage(main, wasIconic, GetWindow(main, GW_HWNDPREV));
        if (wasIconic) ShowWindowAsync(main, SW_SHOWNOACTIVATE);
        SetWindowPos(main, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);

        var bis = Environment.TickCount64 + RaiseTimeoutMs;
        while (Environment.TickCount64 < bis)
        {
            if (!IsIconic(main) && OnTop(main, target)) { lage.Ok = true; break; }
            Thread.Sleep(30);
        }
        return lage;
    }

    /// <summary>
    /// Liegt TCunihockey an der Stelle des Elements zuoberst? Verglichen wird
    /// das Hauptfenster, nicht das Element: ein Label meldet sich bei einer
    /// Trefferprüfung aus einem fremden Prozess als durchlässig, und
    /// WindowFromPoint liefert dann das Fenster darunter. TCunihockey selbst
    /// prüft im eigenen Thread, dort antwortet das Label mit "getroffen".
    /// </summary>
    static bool OnTop(nint main, nint target)
    {
        GetWindowRect(target, out var r);
        var unter = WindowFromPoint(new POINT { X = (r.L + r.R) / 2, Y = (r.T + r.B) / 2 });
        return unter != 0 && (unter == main || GetAncestor(unter, GA_ROOT) == main);
    }

    // ── Menü ─────────────────────────────────────────────────────────────────

    /// <summary>Schliesst ein Menü gezielt an seinem Fenster — keine globale
    /// Taste, die in einer anderen Anwendung landen könnte.</summary>
    static void CloseMenu(nint menu)
    {
        PostMessageW(menu, WM_KEYDOWN, VK_ESCAPE, 0);
        PostMessageW(menu, WM_KEYUP, VK_ESCAPE, 0);
        Thread.Sleep(120);
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

    static int ProcessId()
    {
        var main = TcuWindow.Handle();
        if (main == 0) return 0;
        GetWindowThreadProcessId(main, out var pid);
        return pid;
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

    static nint MakeLParam(int x, int y) => (nint)((y << 16) | (x & 0xFFFF));

    // ── Win32 ────────────────────────────────────────────────────────────────
    const int  WM_CONTEXTMENU = 0x007B;
    const int  WM_KEYDOWN     = 0x0100, WM_KEYUP = 0x0101;
    const int  WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202;
    const int  MK_LBUTTON     = 0x0001;
    const int  VK_ESCAPE      = 0x1B;
    const uint SMTO_ABORTIFHUNG = 0x0002;
    const uint GA_ROOT = 2, GW_HWNDPREV = 3;
    const int  SW_SHOWNOACTIVATE = 4, SW_SHOWMINNOACTIVE = 7;
    const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOACTIVATE = 0x0010;
    const nint HWND_TOPMOST = -1, HWND_NOTOPMOST = -2;

    delegate bool EnumProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, nint lParam);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(nint hwnd, out int pid);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] static extern bool IsIconic(nint hwnd);
    [DllImport("user32.dll")] static extern bool GetWindowRect(nint hwnd, out RECT r);
    [DllImport("user32.dll")] static extern bool GetClientRect(nint hwnd, out RECT r);
    [DllImport("user32.dll")] static extern nint WindowFromPoint(POINT p);
    [DllImport("user32.dll")] static extern nint GetAncestor(nint hwnd, uint flags);
    [DllImport("user32.dll")] static extern nint GetWindow(nint hwnd, uint cmd);
    [DllImport("user32.dll")] static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern bool ShowWindowAsync(nint hwnd, int cmd);
    [DllImport("user32.dll")] static extern bool PostMessageW(nint hwnd, int msg, nint wParam, nint lParam);
    [DllImport("user32.dll")] static extern nint SendMessageTimeoutW(nint hwnd, int msg, nint wParam, nint lParam, uint flags, uint timeout, out nint result);

    [StructLayout(LayoutKind.Sequential)] struct RECT  { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
}
