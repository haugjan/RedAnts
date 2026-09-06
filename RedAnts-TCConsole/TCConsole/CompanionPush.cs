using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TcuConsole;

/// <summary>
/// Pusht Spielernamen via Companion REST API in die Button-Labels.
/// Companion API: POST /api/location/{page}/{row}/{col}/style
/// Companion läuft standardmässig auf Port 8000.
///
/// Seiten und Rasterbelegung kommen aus CompanionConfig, damit der Push
/// exakt die Buttons trifft, die die generierte Config dort angelegt hat.
/// Achtung: der Push ändert nur den Text, nicht die hinterlegte Aktion.
/// </summary>
public class CompanionPush(TcuLogger logger)
{
    static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };

    // Companion-URL: überschreibbar via Umgebungsvariable TCU_COMPANION_URL.
    // Companion lauscht standardmässig auf 8000 (nicht 8888 — das war hier
    // schlicht falsch und liess jeden Push in den Timeout laufen).
    static string CompanionBase =>
        Environment.GetEnvironmentVariable("TCU_COMPANION_URL")
        ?? "http://127.0.0.1:8000";

    public async Task PushAsync(TcuGameState state)
    {
        int sent = 0, failed = 0;

        // Vier Seiten mit Spielerraster: je Mannschaft eine für die allgemeine
        // Spielerwahl und eine für das Tor (die trägt zusätzlich den
        // Eigentor-Knopf und hat deshalb einen Platz weniger).
        foreach (var (page, tor, home) in CompanionConfig.PlayerPages())
        {
            var players = home ? state.HomePlayers : state.AwayPlayers;

            // Passt der Kader überhaupt? Früher fielen zu viele Spieler
            // stillschweigend weg — das darf nicht unbemerkt bleiben.
            var platz = CompanionConfig.PlayerSlotCount(tor);
            if (players.Count > platz)
                logger.Log($"Seite {page}: {players.Count} Spieler, aber nur {platz} Plätze — " +
                           $"die letzten {players.Count - platz} fehlen auf dem Deck", LogLevel.Warning);

            var (s, f) = await PushPlayersToPage(page, players, tor);
            sent += s; failed += f;
        }

        // Die sechs Namen der Starting-6-Seiten. Vorher entstanden sie beim
        // Erzeugen der Config und blieben bis zum nächsten Import stehen — und
        // sie zeigten schlicht die ersten sechs des Kaders statt der echten
        // Startaufstellung.
        foreach (var (page, side) in new[] {
            (CompanionConfig.Starting6PageHome, "home"),
            (CompanionConfig.Starting6PageAway, "away") })
        {
            var six = state.Starting6(side);
            if (six.Count < CompanionConfig.Starting6Count)
                logger.Log($"Startaufstellung {side}: nur {six.Count} von " +
                           $"{CompanionConfig.Starting6Count} Spielern zuzuordnen", LogLevel.Warning);

            foreach (var (col, _, label) in CompanionConfig.Starting6Slots(six))
            {
                if (await SetButtonText(page, CompanionConfig.Starting6Row, col, label)) sent++;
                else failed++;
            }
        }

        if (failed == 0)
            logger.Log($"Companion: {sent} Buttons aktualisiert ({CompanionBase})");
        else
            logger.Log($"Companion: {sent} OK / {failed} fehlgeschlagen — läuft Companion auf {CompanionBase}?",
                       LogLevel.Warning);
    }

    // Nach so vielen Fehlschlägen in Folge gilt Companion als nicht erreichbar.
    // Ohne diese Bremse liefe ein Push bei totem Companion in 40+ Timeouts
    // à 3 s — also minutenlang.
    const int MaxConsecutiveFailures = 3;

    async Task<(int sent, int failed)> PushPlayersToPage(int page, List<Player> players, bool tor)
    {
        int sent = 0, failed = 0, streak = 0;

        // Raster und Beschriftung kommen aus CompanionConfig — identisch zu dem,
        // was die generierte Config auf diese Seite legt. Der Zeilenumbruch ist
        // ein echtes \n; JsonSerializer escaped es korrekt für Companion.
        //
        // Bewusst ALLE Plätze, auch die unbelegten: die bekommen eine leere
        // Beschriftung. Sonst bliebe bei einem kleineren Kader der Name des
        // vorherigen Spiels stehen.
        foreach (var (row, col, _, _, label) in CompanionConfig.PlayerSlots(players, tor))
        {
            if (await SetButtonText(page, row, col, label)) { sent++; streak = 0; }
            else
            {
                failed++;
                if (++streak >= MaxConsecutiveFailures)
                {
                    logger.Log($"Companion: Seite {page} abgebrochen nach {streak} Fehlversuchen in Folge",
                               LogLevel.Warning);
                    break;
                }
            }
        }
        return (sent, failed);
    }

    /// <summary>Beschriftet einen einzelnen Button. Für die Anzeigefelder, die
    /// laufend aktualisiert werden (Spieluhr, Stand) — dort wäre die
    /// Fehlerbremse aus PushPlayersToPage schädlich, weil ein einzelner
    /// Aussetzer die Anzeige dauerhaft stillstellen würde.</summary>
    public Task<bool> PushButtonAsync(int page, int row, int col, string text) =>
        SetButtonText(page, row, col, text);

    /// <summary>
    /// Schreibt in die Kopfzeile aller vier Spielerseiten, wofür der nächste
    /// Spielerdruck gilt — "Tor", "Spieler", "Best Player" oder "Strafe 2'".
    /// Auf allen vier, weil vorher offen ist, welche Seite der Anwender
    /// ansteuert.
    /// </summary>
    public async Task PushModeLabelAsync(string label)
    {
        var (row, col) = CompanionConfig.HeadPos;
        foreach (var (page, _, _) in CompanionConfig.PlayerPages())
            await SetButtonText(page, row, col, label);
    }

    /// <summary>
    /// Setzt eine Companion-Custom-Variable. Darüber läuft die Rückmeldung des
    /// Zustands aufs Deck: TcuConsole schreibt den Wert, die Knöpfe tragen ein
    /// Feedback darauf und färben sich selbst.
    ///
    /// Bewusst über die Variable statt über einen Style-Push auf den Knopf:
    /// die Variable gilt für beliebig viele Knöpfe auf beliebig vielen Seiten
    /// und überlebt einen erneuten Config-Import, während ein Style-Push nur
    /// die eine Position trifft.
    ///
    /// Route geprüft an Companion 5.0.3: POST /api/custom-variable/{name}/value
    /// mit dem Wert als Rumpf antwortet "ok", GET liest ihn zurück.
    /// </summary>
    public async Task<bool> SetVariableAsync(string name, string value)
    {
        try
        {
            var url  = $"{CompanionBase}/api/custom-variable/{Uri.EscapeDataString(name)}/value";
            var body = new StringContent(value, Encoding.UTF8, "text/plain");
            var resp = await _http.PostAsync(url, body);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    async Task<bool> SetButtonText(int page, int row, int col, string text)
    {
        try
        {
            var url  = $"{CompanionBase}/api/location/{page}/{row}/{col}/style";
            var body = new StringContent(
                JsonSerializer.Serialize(new { text }),
                Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(url, body);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
