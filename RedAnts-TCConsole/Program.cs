using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using TcuConsole;

Console.OutputEncoding = Encoding.UTF8;
Console.Title = "TCU Console";

var udp      = new TcuUdp();
var state    = new TcuGameState();
var logger   = new TcuLogger();
var reader   = new TcuStateReader(state, logger);
var ui       = new TcuUi(logger);
var live     = new TcuState(logger);
var lower    = new TcuLower(udp, ui, live, state, logger);
var bridge   = new TcuUiBridge(lower, logger);
var deck     = new TcuDeck(udp, lower, state, logger);

logger.PrintBanner();

const string BaseUrl = "http://localhost:5150/";

// Spielkonfig laden: UI Automation liefert nur Teamnamen, der Kader kommt
// ausschliesslich aus der Spielkonfig-Datei — deshalb immer beides ausführen.
var tcuDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));

_ = Task.Run(async () =>
{
    await Task.Delay(300);
    await LoadStateAsync(reader, state, tcuDir);

    // Shortcut- und Kartenbeschriftung stehen in der System-Konfig, die
    // TcuConsole nicht liest — sie kommen aus dem laufenden Fenster.
    ui.ReadLabels(state);
    logger.PrintState(state);

    // Der Kader steht jetzt: das Deck zeigt die Spielernamen erst danach.
    deck.Bump();

    logger.PrintDownloadInfo(BaseUrl, state);
    logger.Log("Bereit. Warte auf Befehle von Companion...");
    logger.Log("");
});

// Minimaler HTTP-Listener (kein Web-Framework)
//
// Zusätzlich zu "localhost" auch 127.0.0.1: http.sys prüft den Host-Kopf gegen
// das Präfix und beantwortet eine Anfrage an 127.0.0.1 sonst mit 400 — was im
// Companion-Modul wie ein Fehler des Moduls aussähe. Das Präfix auf eine feste
// IP braucht je nach Rechner eine URL-Reservierung; scheitert es daran, läuft
// der Listener eben nur auf localhost weiter.
var listener = new HttpListener();
listener.Prefixes.Add(BaseUrl);
listener.Prefixes.Add("http://127.0.0.1:5150/");
try
{
    listener.Start();
}
catch (HttpListenerException)
{
    listener = new HttpListener();
    listener.Prefixes.Add(BaseUrl);
    listener.Start();
    logger.Log("127.0.0.1 nicht reservierbar — im Modul muss localhost stehen", LogLevel.Warning);
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); listener.Stop(); };

logger.Log($"HTTP-Listener auf {BaseUrl}");

// UDP-Brücke für die Funktionen ohne UDP-Befehl (Strafen, Karten, Shortcuts,
// Mitteilung). Läuft neben dem HTTP-Listener; fällt sie aus, bleibt alles
// andere bedienbar.
_ = Task.Run(() => bridge.RunAsync(cts.Token));

// Spielzustand laufend aufs Deck: liest aus dem Fenster von TCunihockey. Das
// Deck meldet die Änderung an die wartenden Long-Polls weiter — fällt Companion
// aus, bleibt die Steuerung trotzdem bedienbar.
//
// Die Zuschauerzahl wird nur EINMAL übernommen, und nur wenn TcuConsole selbst
// noch keine kennt: im Mitteilungsfeld steht, was TcuConsole zuletzt dort
// hineingeschrieben hat, also der eigene Zähler nach einem Neustart. Später
// darf das Feld den laufenden Zähler nicht mehr überschreiben — dort steht dann
// womöglich längst eine Störungsmeldung.
var spectatorsTaken = false;

// Erkennung eines Spielwechsels. Die Mannschaftsnamen stehen im Fenster und
// kosten nichts; der Kader dagegen ist nur über den Heap von TCunihockey
// lesbar und braucht Sekunden — der wird deshalb erst geholt, wenn sich die
// Namen ändern.
var lastMatch    = "";
var reloadRuns   = 0;

async Task PushSnapshot(TcuState.Snapshot s)
{
    // Anderes Spiel geladen? Dann Kader neu einlesen und alle Beschriftungen
    // nachziehen. Beim allerersten Takt nicht — da hat der Start das schon
    // erledigt.
    if (s.Match != lastMatch)
    {
        var erster = lastMatch.Length == 0;
        lastMatch = s.Match;

        if (!erster && Interlocked.Exchange(ref reloadRuns, 1) == 0)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    logger.Log($"Neues Spiel erkannt: {s.Match} — Kader wird eingelesen...");
                    if (await LoadStateAsync(reader, state, tcuDir))
                    {
                        ui.ReadLabels(state);
                        logger.PrintState(state);
                        deck.Bump();
                    }
                    else logger.Log("Kader konnte nicht gelesen werden", LogLevel.Warning);
                }
                finally { Interlocked.Exchange(ref reloadRuns, 0); }
            });
        }
    }

    // Zuschauerzahl: EINMAL beim Start aus dem Mitteilungsfeld übernehmen —
    // dort steht, was TcuConsole zuletzt selbst hineingeschrieben hat. Danach
    // führt TcuConsole den Zähler, denn im Feld steht später womöglich längst
    // eine Störungsmeldung.
    if (!spectatorsTaken && s.Spectators is int n)
    {
        spectatorsTaken = true;
        if (state.Spectators == 0 && n > 0)
        {
            state.Spectators = n;
            logger.Log($"Zuschauerzahl aus TCunihockey übernommen: {n}");
        }
    }

    // Stand, Drittel und die Farben der Zustandsknöpfe stecken jetzt in der
    // Ansicht. Das Deck vergleicht selbst und weckt die Long-Polls nur, wenn
    // sich davon etwas geändert hat.
    deck.Update(s);
    await Task.CompletedTask;
}

_ = Task.Run(() => live.RunAsync(PushSnapshot, cts.Token));

// Nach jedem Vorgang sofort nachziehen, statt bis zum nächsten Takt zu warten:
// ein Knopf, der eine halbe Sekunde später umfärbt, wirkt wie ein Aussetzer.
lower.StateChanged = async () =>
{
    var s = live.Read();
    if (s is not null) await PushSnapshot(s);
    deck.Bump();
};

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        HttpListenerContext ctx;
        try { ctx = await listener.GetContextAsync(); }
        catch (HttpListenerException) { break; }

        _ = Task.Run(() => HandleRequest(ctx, state, udp, ui, lower, logger, reader, deck, tcuDir, cts, listener));
    }
}
finally
{
    listener.Stop();
    logger.Log("Beendet.");
}

// ── Spielzustand laden ─────────────────────────────────────────────────────
// Vorrang hat TCunihockey selbst: TryReloadFromUiAsync liest Teamnamen aus dem
// Fenster und den Kader aus dem Heap des laufenden Prozesses. Das ist die
// einzige Quelle, die zeigt, welches Spiel TCunihockey WIRKLICH geladen hat.
//
// Die Spielkonfig-Datei ist nur Rückfall für den Fall, dass TCunihockey nicht
// läuft oder der Heap nicht lesbar ist. Sie darf einen erfolgreichen
// Speicher-Read nicht überschreiben — im Ordner kann durchaus die Konfig eines
// anderen Spiels liegen (Auto-Discover nimmt schlicht die neueste gültige).
static async Task<bool> LoadStateAsync(TcuStateReader reader, TcuGameState state, string tcuDir)
{
    var fromApp = await reader.TryReloadFromUiAsync();
    if (fromApp && state.HomePlayers.Count + state.AwayPlayers.Count > 0) return true;

    var fromFile = await reader.AutoDiscoverAsync(tcuDir);
    return fromApp || fromFile;
}

// ── Request-Handler ────────────────────────────────────────────────────────
static async Task HandleRequest(
    HttpListenerContext ctx,
    TcuGameState state, TcuUdp udp, TcuUi ui, TcuLower lower, TcuLogger logger,
    TcuStateReader reader, TcuDeck deck, string tcuDir,
    CancellationTokenSource cts, HttpListener listener)
{
    var req  = ctx.Request;
    var resp = ctx.Response;
    resp.Headers.Add("Access-Control-Allow-Origin", "*");
    resp.ContentType = "application/json; charset=utf-8";

    try
    {
        var path   = req.Url?.AbsolutePath.TrimEnd('/') ?? "/";
        var method = req.HttpMethod;

        if (method == "OPTIONS") { resp.StatusCode = 204; resp.Close(); return; }

        object? result = null;
        int status = 200;

        // ── Deck ─────────────────────────────────────────────────────────────
        // Die Ansicht wird gehalten, bis sich etwas ändert oder die Wartezeit
        // abläuft. 25 s liegen unter dem Timeout des Moduls (40 s), damit dort
        // keine Anfrage abbricht.
        if (method == "GET" && path == "/deck/view")
        {
            var since = long.TryParse(req.QueryString["since"], out var v) ? v : 0;
            await deck.WaitAsync(since, TimeSpan.FromSeconds(25), cts.Token);
            result = deck.View();
        }
        // Tastenbilder einzeln und nur auf Anfrage: 32 Base64-Bilder bei jedem
        // Takt wären ein Vielfaches der Ansicht selbst. Das Modul merkt sich
        // jedes Bild unter seinem Schlüssel.
        else if (method == "GET" && path == "/deck/image")
        {
            result = new { image = TcuDeck.Image(req.QueryString["key"] ?? "") };
        }
        else if (method == "POST" && path.StartsWith("/deck/press/"))
        {
            if (!int.TryParse(path["/deck/press/".Length..], out var slot))
            {
                status = 400;
                result = new { error = "Keine Tastennummer" };
            }
            else
            {
                var note = await deck.PressAsync(slot);
                result = new { ok = note is null, note };
            }
        }
        else if (method == "GET" && path == "/state")
        {
            result = state.ToDto();
        }
        else if (method == "GET" && path.StartsWith("/players/"))
        {
            var side = path["/players/".Length..];
            result = side.Equals("away", StringComparison.OrdinalIgnoreCase)
                ? state.AwayPlayers : state.HomePlayers;
        }
        else if (method == "POST" && path == "/state/reload")
        {
            var ok = await LoadStateAsync(reader, state, tcuDir);
            ui.ReadLabels(state);
            logger.PrintState(state);
            deck.Bump();
            result = new { success = ok };
        }
        else if (method == "POST" && path == "/state/load")
        {
            var body = await ReadBodyAsync(req);
            var doc  = JsonDocument.Parse(body);
            var file = doc.RootElement.GetProperty("path").GetString() ?? "";
            var ok   = await reader.LoadFromFileAsync(file);
            logger.PrintState(state);
            deck.Bump();
            if (!ok) status = 400;
            result = new { success = ok };
        }
        else if (method == "POST" && path == "/action")
        {
            var body   = await ReadBodyAsync(req);
            var actReq = JsonSerializer.Deserialize<ActionRequest>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (actReq is null) { status = 400; result = new { error = "Ungültige Anfrage" }; }
            else
            {
                var cmds = TcuActions.Resolve(actReq, state);
                if (cmds.Length == 0) { status = 400; result = new { error = $"Unbekannte Aktion: {actReq.Type}" }; }
                else
                {
                    logger.LogAction(actReq, cmds, state);
                    var notes = await DispatchAsync(cmds, udp, lower);
                    result = new { sent = cmds.Length, notes = notes.Count == 0 ? null : notes.ToArray() };
                }
            }
        }
        else if (method == "POST" && path == "/shutdown")
        {
            resp.StatusCode = 200;
            var bye = Encoding.UTF8.GetBytes("{\"ok\":true}");
            resp.ContentLength64 = bye.Length;
            await resp.OutputStream.WriteAsync(bye);
            resp.Close();
            cts.Cancel();
            listener.Stop();
            return;
        }
        else if (method == "POST" && path == "/send")
        {
            var body    = await ReadBodyAsync(req);
            var rawReq  = JsonSerializer.Deserialize<RawRequest>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (string.IsNullOrWhiteSpace(rawReq?.Command))
                { status = 400; result = new { error = "Kein Befehl" }; }
            else
            {
                logger.LogRaw(rawReq.Command);
                var notes = await DispatchAsync([rawReq.Command], udp, lower);
                result = new { ok = notes.Count == 0, notes = notes.ToArray() };
            }
        }
        else if (method == "GET" && path == "/ui/probe")
        {
            // Reine Diagnose: sucht alle Bedienelemente im TCunihockey-Fenster
            // und meldet, was gefunden wurde — ohne einen einzigen Klick.
            result = ui.Probe();
        }
        else
        {
            status = 404;
            result = new { error = "Nicht gefunden" };
        }

        resp.StatusCode = status;
        var json = JsonSerializer.SerializeToUtf8Bytes(result);
        resp.ContentLength64 = json.Length;
        await resp.OutputStream.WriteAsync(json);
    }
    catch (Exception ex)
    {
        resp.StatusCode = 500;
        var err = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = ex.Message }));
        await resp.OutputStream.WriteAsync(err);
    }
    finally
    {
        resp.Close();
    }
}

// ── Befehle verteilen ──────────────────────────────────────────────────────
// "TcuUi=" geht an die Ablaufsteuerung (die kümmert sich um Ausblenden,
// Wartezeit und Einblenden), alles andere direkt per UDP an TCunihockey.
static async Task<List<string>> DispatchAsync(string[] cmds, TcuUdp udp, TcuLower lower)
{
    var notes = new List<string>();
    foreach (var cmd in cmds)
    {
        if (cmd.StartsWith("TcuUi=", StringComparison.OrdinalIgnoreCase))
        {
            var parts = cmd["TcuUi=".Length..].Split('|');
            var note  = await lower.ExecuteAsync(parts[0].Trim().ToLowerInvariant(), parts);
            if (note is not null) notes.Add($"{cmd}: {note}");
        }
        else
        {
            udp.Send(cmd);
        }
    }
    return notes;
}

static async Task<string> ReadBodyAsync(HttpListenerRequest req)
{
    using var sr = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
    return await sr.ReadToEndAsync();
}
