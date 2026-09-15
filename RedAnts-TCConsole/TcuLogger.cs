namespace TcuConsole;

public enum LogLevel { Info, Warning, Error }

public class TcuLogger
{
    private readonly object _lock = new();

    public void PrintBanner(TcuGameState? state = null)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           TCU Console  ·  TCUnihockey v6.2.10           ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        PrintSeparator();
    }

    public void PrintSeparator()
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("──────────────────────────────────────────────────────────");
            Console.ResetColor();
        }
    }

    public void PrintState(TcuGameState state)
    {
        lock (_lock)
        {
            if (state.LoadedAt.HasValue)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"  {state.HomeTeam}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(" vs ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(state.AwayTeam);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  Heim: {state.HomePlayers.Count} Spieler  |  Gast: {state.AwayPlayers.Count} Spieler");
                if (!string.IsNullOrEmpty(state.Commentator1))
                    Console.WriteLine($"  Kommentar: {state.Commentator1}, {state.Commentator2}");
                if (!string.IsNullOrEmpty(state.Referee1))
                    Console.WriteLine($"  Schiedsrichter: {state.Referee1}, {state.Referee2}");
                if (!string.IsNullOrEmpty(state.HomeTopScorer) || !string.IsNullOrEmpty(state.AwayTopScorer))
                    Console.WriteLine($"  Topscorer (00=): {state.HomeTopScorer} | {state.AwayTopScorer}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  Kein Spielkonfig geladen — POST /state/reload oder /state/load");
            }
            Console.ResetColor();
            PrintSeparator();
        }
    }

    // ── Wo die Companion-Config abzuholen ist ────────────────────────────────
    public void PrintDownloadInfo(string baseUrl, TcuGameState state)
    {
        lock (_lock)
        {
            var url   = baseUrl.TrimEnd('/') + "/deck/view";
            var kader = state.HomePlayers.Count + state.AwayPlayers.Count;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  Companion-Modul — Server-URL:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    {baseUrl.TrimEnd('/')}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"    Ansicht der 32 Tasten: {url}");
            Console.WriteLine("    Im Modul RedAnts TCConsole eintragen; die Tasten 1–32 werden");
            Console.WriteLine("    einmalig zugewiesen und bleiben danach unverändert.");

            if (kader == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("    Achtung: kein Kader geladen — die Spielerwahl bliebe leer.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"    Kader: {kader} Spieler. Die Namen stehen live auf den Tasten.");
            }

            Console.ResetColor();
            PrintSeparator();
        }
    }

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
            Console.ForegroundColor = level switch
            {
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error   => ConsoleColor.Red,
                _                => ConsoleColor.Gray,
            };
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    public void LogAction(ActionRequest req, string[] cmds, TcuGameState state)
    {
        lock (_lock)
        {
            var side   = req.Side ?? "home";
            var team   = side.Equals("away", StringComparison.OrdinalIgnoreCase)
                ? state.AwayTeam : state.HomeTeam;
            var player = req.Nr.HasValue ? state.FindPlayer(side, req.Nr.Value) : null;
            var pLabel = player != null
                ? $"#{req.Nr} {player.Name}"
                : req.Nr.HasValue ? $"#{req.Nr}" : "";

            var (icon, color) = req.Type?.ToLower() switch
            {
                "tor"        => ("⚽", ConsoleColor.Green),
                "strafe"     => ("🟨", ConsoleColor.Yellow),
                "name"       => ("👤", ConsoleColor.White),
                "bestplayer" => ("⭐", ConsoleColor.DarkYellow),
                "coach"      => ("🎽", ConsoleColor.White),
                "timeout"    => ("⏱ ", ConsoleColor.Magenta),
                "starting6"  => ("6️⃣ ", ConsoleColor.Cyan),
                "lineup"     => ("📋", ConsoleColor.Cyan),
                "commentary" => ("🎙 ", ConsoleColor.White),
                "referee"    => ("🟡", ConsoleColor.Yellow),
                "hide"       => ("⬛", ConsoleColor.DarkGray),
                "opener"     => ("🎬", ConsoleColor.White),
                "result"     => ("📊", ConsoleColor.White),
                _            => ("▶ ", ConsoleColor.White),
            };

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
            Console.ForegroundColor = color;
            Console.Write($"{icon} {req.Type?.ToUpper()}");

            if (req.Type?.ToLower() is not ("hide" or "commentary" or "referee" or "opener" or "result"))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($" {team}");
            }

            if (!string.IsNullOrEmpty(pLabel))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($" — {pLabel}");
            }
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            foreach (var cmd in cmds)
                Console.WriteLine($"           → {StripPrefix(cmd)}");
            Console.ResetColor();
        }
    }

    // Seit es neben "TcuController=" auch "TcuUi=" gibt, darf hier nicht mehr
    // blind eine feste Anzahl Zeichen abgeschnitten werden.
    static string StripPrefix(string cmd)
    {
        var eq = cmd.IndexOf('=');
        return eq > 0 ? cmd[(eq + 1)..] : cmd;
    }

    public void LogUi(string command)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] UI  → ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(StripPrefix(command));
            Console.ResetColor();
        }
    }

    public void LogRaw(string command)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] RAW → ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(StripPrefix(command));
            Console.ResetColor();
        }
    }
}
