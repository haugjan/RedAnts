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

    // ── Aus dem laufenden TCunihockey ────────────────────────────────────────
    // Einzige Quelle für Kader, Startaufstellung und Mannschaftsnamen — alles
    // aus dem Heap von TCunihockey, die Namen über die Fenster-Handles ihrer
    // Labels (siehe TcuMemory).
    //
    // Früher kamen die Namen über UI Automation, und fehlte der Heimname, brach
    // das Einlesen ab, bevor der Kader gelesen war. Bei minimiertem TCunihockey
    // findet UI Automation diese Labels nicht; dann griff der Rückfall auf die
    // neueste Spielkonfig-Datei im Ordner und lud eine Beispielkonfig mit fremdem
    // Kader. Den Rückfall gibt es nicht mehr: ist der Kader nicht lesbar, bleibt
    // die Spielerwahl leer — ein leeres Deck fällt auf, ein falscher Kader nicht.
    public Task<bool> TryReloadFromUiAsync() => Task.Run(() =>
    {
        try
        {
            if (TcuWindow.Handle() == 0)
            {
                logger.Log("Bedienfenster von TCUnihockey nicht gefunden", LogLevel.Warning);
                Clear();
                return false;
            }

            var mem = new TcuMemory(logger).TryRead();
            if (mem is null || mem.HomePlayers.Count + mem.AwayPlayers.Count == 0)
            {
                logger.Log("Kader aus TCUnihockey nicht lesbar — die Spielerwahl bleibt leer", LogLevel.Warning);
                Clear();
                return false;
            }

            state.HomeTeam      = mem.HomeTeam;
            state.AwayTeam      = mem.AwayTeam;
            state.HomeTeamShort = mem.HomeTeamShort;
            state.AwayTeamShort = mem.AwayTeamShort;
            state.HomePlayers   = mem.HomePlayers;
            state.AwayPlayers   = mem.AwayPlayers;
            // Startaufstellung als Nummernliste, z.B. "46,6,00,10,12,14".
            state.HomeStarting6 = SplitNumbers(mem.Starting6Home);
            state.AwayStarting6 = SplitNumbers(mem.Starting6Away);
            state.LoadedAt      = DateTime.Now;

            logger.Log($"Aus TCUnihockey gelesen: {state.HomeTeam} vs {state.AwayTeam} " +
                       $"({mem.HomePlayers.Count + mem.AwayPlayers.Count} Spieler, " +
                       $"Stand {mem.ScoreHome}:{mem.ScoreAway})");
            return true;
        }
        catch (Exception ex)
        {
            logger.Log($"Einlesen aus TCUnihockey fehlgeschlagen: {ex.Message}", LogLevel.Error);
            Clear();
            return false;
        }
    });

    /// <summary>Kein lesbarer Kader heisst: keiner. Stehen bleiben darf der
    /// alte nicht — nach einem Spielwechsel wäre er der falsche.</summary>
    void Clear()
    {
        state.HomePlayers   = [];
        state.AwayPlayers   = [];
        state.HomeStarting6 = [];
        state.AwayStarting6 = [];
    }

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
