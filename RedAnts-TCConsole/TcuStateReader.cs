using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace TcuConsole;

public class TcuStateReader(TcuGameState state, TcuLogger logger)
{
    // "00" bleibt bewusst drin: TCunihockey führt es selbst als Kadereintrag
    // (nachgewiesen in lstHomePlayers/lstAwayPlayers) und referenziert es in
    // Starting6 und den Linien. Entscheidend ist, die Nummer als Text zu
    // behalten — als Zahl würde daraus 0 und der Spieler wäre über
    // lowerthird_{side}_player|0 nicht ansprechbar.
    static readonly HashSet<string> SkipKeys = new(StringComparer.OrdinalIgnoreCase)
        { "TeamLong", "TeamShort", "Coach", "Starting6", "Line1", "Line2", "Line3", "Line4", "Goal" };

    // ── UI Automation: liest Teamnamen aus dem laufenden TCUnihockey ──────────
    public Task<bool> TryReloadFromUiAsync() => Task.Run(() =>
    {
        try
        {
            // Über TcuWindow statt MainWindowHandle: hat TCunihockey ein
            // Meldungsfenster offen, zeigt MainWindowHandle auf dieses und die
            // Teamnamen wären nicht auffindbar.
            var hwnd = TcuWindow.Handle();
            if (hwnd == 0)
            {
                logger.Log("Bedienfenster von TCUnihockey nicht gefunden", LogLevel.Warning);
                return false;
            }

            var root = AutomationElement.FromHandle(hwnd);
            if (root is null) return false;

            string ReadById(string id)
            {
                try
                {
                    var el = root.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.AutomationIdProperty, id));
                    return el?.Current.Name.Trim() ?? "";
                }
                catch { return ""; }
            }

            var homeTeam = ReadById("LblHome");
            var awayTeam = ReadById("LblAway");

            if (string.IsNullOrWhiteSpace(homeTeam)) return false;

            state.HomeTeam      = homeTeam;
            state.AwayTeam      = awayTeam;
            state.HomeTeamShort = ReadById("LblScoreboardHome");
            state.AwayTeamShort = ReadById("LblScoreboardAway");
            state.LoadedAt      = DateTime.Now;

            // Kader: UI Automation sieht ihn nicht (er existiert nur als
            // ContextMenuStrip, dessen Einträge erst beim Öffnen im Baum
            // auftauchen). Deshalb direkt aus dem Heap von TCunihockey.
            var mem = new TcuMemory(logger).TryRead();
            if (mem is not null && (mem.HomePlayers.Count > 0 || mem.AwayPlayers.Count > 0))
            {
                state.HomePlayers   = mem.HomePlayers;
                state.AwayPlayers   = mem.AwayPlayers;
                // Startaufstellung als Nummernliste, z.B. "46,6,00,10,12,14".
                state.HomeStarting6 = SplitNumbers(mem.Starting6Home);
                state.AwayStarting6 = SplitNumbers(mem.Starting6Away);
                logger.Log($"Aus TCUnihockey gelesen: {state.HomeTeam} vs {state.AwayTeam} " +
                           $"({mem.HomePlayers.Count + mem.AwayPlayers.Count} Spieler aus dem Speicher, " +
                           $"Stand {mem.ScoreHome}:{mem.ScoreAway})");
            }
            else
            {
                logger.Log($"Aus TCUnihockey gelesen: {state.HomeTeam} vs {state.AwayTeam} " +
                           "(Kader nicht lesbar — Rückfall auf die Spielkonfig-Datei)", LogLevel.Warning);
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.Log($"UI Automation Fehler: {ex.Message}", LogLevel.Error);
            return false;
        }
    });

    // ── Datei: liest vollständigen Spielkonfig (inkl. Spieler) ───────────────
    public async Task<bool> LoadFromFileAsync(string path)
    {
        if (!File.Exists(path))
        {
            logger.Log($"Datei nicht gefunden: {path}", LogLevel.Error);
            return false;
        }
        var text = await File.ReadAllTextAsync(path);
        return ParseGameConfig(text);
    }

    // ── Auto-Discover: neueste Spielkonfig im TCUnihockey-Ordner ─────────────
    // Durchsucht den Hauptordner UND Configurations\ (dort legt TCUnihockey
    // die Spielkonfigs ab). Neueste gültige Datei gewinnt.
    public async Task<bool> AutoDiscoverAsync(string directory)
    {
        string[] dirs = [directory, Path.Combine(directory, "Configurations")];

        var candidates = new List<FileInfo>();
        foreach (var dir in dirs)
        {
            try
            {
                if (!Directory.Exists(dir)) continue;
                candidates.AddRange(Directory
                    .GetFiles(dir, "*.txt")
                    .Select(f => new FileInfo(f))
                    // leere Platzhalter und offensichtliche Nicht-Configs überspringen
                    .Where(f => f.Length is > 0 and < 1_000_000));
            }
            catch (Exception ex)
            {
                logger.Log($"Auto-Discover: {dir} nicht lesbar ({ex.Message})", LogLevel.Warning);
            }
        }

        foreach (var fi in candidates.OrderByDescending(f => f.LastWriteTime))
        {
            string text;
            try { text = await File.ReadAllTextAsync(fi.FullName); }
            catch { continue; }

            if (text.Contains("[Game-Config]") && text.Contains("[Home]"))
            {
                logger.Log($"Auto-Discover: {fi.FullName}");
                return ParseGameConfig(text);
            }
        }

        logger.Log($"Auto-Discover: keine gültige Spielkonfig gefunden " +
                   $"({candidates.Count} .txt geprüft in {string.Join(", ", dirs)})", LogLevel.Warning);
        return false;
    }

    public bool ParseGameConfig(string text)
    {
        var sections = ParseIni(text);
        if (!sections.ContainsKey("Game-Config")) return false;

        var game = sections.GetValueOrDefault("Game")  ?? [];
        var home = sections.GetValueOrDefault("Home")  ?? [];
        var away = sections.GetValueOrDefault("Away")  ?? [];

        state.HomeTeam      = home.GetValueOrDefault("TeamLong",     "HEIM").ToUpper();
        state.AwayTeam      = away.GetValueOrDefault("TeamLong",     "GAST").ToUpper();
        state.HomeTeamShort = home.GetValueOrDefault("TeamShort",    "HEI").ToUpper();
        state.AwayTeamShort = away.GetValueOrDefault("TeamShort",    "GAS").ToUpper();
        state.HomeCoach     = home.GetValueOrDefault("Coach",        "");
        state.AwayCoach     = away.GetValueOrDefault("Coach",        "");
        state.HomeTopScorer = home.GetValueOrDefault("00",           "").Trim();
        state.AwayTopScorer = away.GetValueOrDefault("00",           "").Trim();
        state.Commentator1  = game.GetValueOrDefault("Commentator1", "");
        state.Commentator2  = game.GetValueOrDefault("Commentator2", "");
        state.Referee1      = game.GetValueOrDefault("Referee1",     "");
        state.Referee2      = game.GetValueOrDefault("Referee2",     "");

        state.HomePlayers   = ExtractPlayers(home);
        state.AwayPlayers   = ExtractPlayers(away);
        state.HomeStarting6 = SplitNumbers(home.GetValueOrDefault("Starting6", ""));
        state.AwayStarting6 = SplitNumbers(away.GetValueOrDefault("Starting6", ""));
        state.LoadedAt      = DateTime.Now;

        logger.Log($"Geladen: {state.HomeTeam} vs {state.AwayTeam} " +
                   $"({state.HomePlayers.Count + state.AwayPlayers.Count} Spieler)");
        return true;
    }

    /// <summary>Zerlegt "46,6,00,10,12,14" in die einzelnen Nummern. Als Text,
    /// damit "00" nicht zu "0" wird — sonst wäre der Eintrag nicht mehr dem
    /// Spieler zuzuordnen. Leerstellen wie in "17,-,00,9,22" fallen weg.</summary>
    static List<string> SplitNumbers(string value) =>
        value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
             .Where(s => s != "-")
             .ToList();

    static List<Player> ExtractPlayers(Dictionary<string, string> section)
    {
        var all = section
            .Where(kv => !SkipKeys.Contains(kv.Key) && Regex.IsMatch(kv.Key, @"^\d+$"))
            .Select(kv => new Player
            {
                NrText = kv.Key.Trim(),
                Nr     = int.TryParse(kv.Key, out var n) ? n : 0,
                Name   = kv.Value.Trim(),
            })
            .Where(p => p.Name.Length > 0)
            .ToList();

        return all.OrderBy(p => p.Nr).ToList();
    }

    static Dictionary<string, Dictionary<string, string>> ParseIni(string text)
    {
        var result  = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string? sec = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var m = Regex.Match(line, @"^\[(.+)\]$");
            if (m.Success) { sec = m.Groups[1].Value; result[sec] = new(StringComparer.OrdinalIgnoreCase); continue; }
            if (sec is null) continue;
            var eq = line.IndexOf('=');
            if (eq > 0) result[sec][line[..eq].Trim()] = line[(eq + 1)..].Trim();
        }
        return result;
    }
}
