using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcuConsole;

/// <summary>
/// Nimmt "TcuUi=..."-Befehle per UDP entgegen und übergibt sie der
/// Ablaufsteuerung.
///
/// Eigener Port neben TCunihockey (7001), bewusst dasselbe Protokoll und
/// dasselbe Companion-Modul (generic-tcp-udp): in der Companion-Config ist
/// das nur eine zweite Verbindung mit anderer Portnummer, kein zweites Modul,
/// dessen Feldnamen wieder erraten werden müssten.
///
/// Über diese Brücke laufen jetzt sämtliche Einblendungen, nicht mehr nur die
/// Funktionen ohne UDP-Befehl. Grund ist die Reihenfolge: TCunihockey nimmt
/// keinen Moduswechsel an, solange etwas eingeblendet ist, und das Ausblenden
/// dauert 2,2 Sekunden. Das lässt sich nur mit gelesenem Zustand steuern,
/// nicht mit den festen Verzögerungen, die Companion kann (siehe TcuLower).
///
/// Direkt an 7001 bleiben Matchuhr, Drittel und PANIC — die brauchen keine
/// Reihenfolge und funktionieren dadurch auch dann, wenn TcuConsole steht.
/// </summary>
public class TcuUiBridge(TcuLower lower, TcuLogger logger)
{
    public const int Port = 7002;

    public async Task RunAsync(CancellationToken token)
    {
        UdpClient client;
        try
        {
            client = new UdpClient(new IPEndPoint(IPAddress.Loopback, Port));
        }
        catch (SocketException ex)
        {
            logger.Log($"UI-Brücke: Port {Port} nicht belegbar ({ex.Message}) — " +
                       "Strafen, Karten, Shortcuts und Mitteilungen bleiben ohne Funktion",
                       LogLevel.Error);
            return;
        }

        using (client)
        {
            logger.Log($"UI-Brücke auf udp://127.0.0.1:{Port}");

            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult packet;
                try { packet = await client.ReceiveAsync(token); }
                catch (OperationCanceledException) { break; }
                catch (SocketException) { break; }

                var text = Encoding.UTF8.GetString(packet.Buffer).Trim('\r', '\n', ' ', '\0');
                if (text.Length == 0) continue;

                // Ein Vorgang nach dem anderen: ein Wechsel dauert bis zu 2,4
                // Sekunden, weil auf die Ausblendung gewartet werden muss.
                // Würde hier pro Paket ein Task starten, käme der zweite
                // Vorgang mitten in die Wartezeit des ersten. Die Reihenfolge
                // sichert zusätzlich TcuLower selbst ab.
                try { await DispatchAsync(text); }
                catch (Exception ex) { logger.Log($"UI-Befehl fehlgeschlagen: {ex.Message}", LogLevel.Error); }
            }
        }
    }

    async Task DispatchAsync(string command)
    {
        if (!command.StartsWith("TcuUi=", StringComparison.OrdinalIgnoreCase))
        {
            logger.Log($"UI-Brücke: unbekanntes Präfix, verworfen — {command}", LogLevel.Warning);
            return;
        }

        var parts = command["TcuUi=".Length..].Split('|');
        var verb  = parts[0].Trim().ToLowerInvariant();

        logger.LogUi(command);

        var note = await lower.ExecuteAsync(verb, parts);
        if (note is not null)
            logger.Log($"UI-Befehl '{command}': {note}", LogLevel.Warning);
    }
}
