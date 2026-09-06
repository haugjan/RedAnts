namespace TcuConsole;

/// <summary>
/// Führt jeden Einblende-Vorgang in der richtigen Reihenfolge aus.
///
/// Warum das nicht in Companion gehen kann — alles gemessen an v6.2.12:
///
///  1. Solange etwas eingeblendet ist, nimmt TCunihockey KEINEN Moduswechsel
///     an. Ein "lowerthird_referee12" bei laufendem Opener lässt die Vorschau
///     unverändert; sichtbar auch daran, dass 80 Bedienelemente gesperrt sind.
///  2. Das Ausblenden dauert rund 2,2 Sekunden. Ein Moduswechsel in dieser
///     Zeit verpufft: mit 0, 500, 1000 und 1500 ms Wartezeit war der Vorgang
///     in 0 von 3 Versuchen erfolgreich, ab 2000 ms in 3 von 3.
///  3. Die Sperre löst sich schon 20 ms nach dem Ausblenden — sie taugt also
///     als Anzeige "läuft etwas", nicht als Anzeige "Ausblendung fertig".
///
/// Companion kann nur feste Verzögerungen. Eine feste Wartezeit von 2,3 s auf
/// jedem Knopf wäre im Spiel unzumutbar, und ohne sie funktioniert der Wechsel
/// nicht. Deshalb entscheidet hier der gelesene Zustand: läuft nichts, geht es
/// ohne jede Verzögerung; läuft etwas, wird ausgeblendet und gewartet.
/// </summary>
public sealed class TcuLower(
    TcuUdp udp, TcuUi ui, TcuState reader, TcuGameState game, TcuLogger logger)
{
    const string P = "TcuController=";

    /// <summary>Wartezeit nach dem Ausblenden. Die Ausblendung dauert gemessen
    /// 2213 ms; ab 2000 ms war der anschliessende Wechsel in allen Versuchen
    /// erfolgreich. 2300 ms lässt etwas Luft.</summary>
    public const int FadeMs = 2300;

    /// <summary>Pause zwischen Moduswechsel und Einblenden.</summary>
    const int ArmMs = 120;

    // Ein Vorgang nach dem anderen. Zwei verschränkte Abläufe würden sich die
    // Wartezeit gegenseitig zerschneiden — der zweite käme mitten in die
    // Ausblendung des ersten.
    readonly SemaphoreSlim _gate = new(1, 1);

    // Vorgemerkte Aktion für die Spielerwahl. Gesetzt von "prepare", verbraucht
    // von "player".
    string _pendingKind = "";
    string _pendingSide = "home";

    // Selbst befohlener Werbungs-Zustand, solange die Anzeige im Fenster noch
    // nachläuft (siehe "sponsor").
    bool _sponsorAssumed;
    long _sponsorAssumedTil;

    /// <summary>Läuft Werbung? Während der Ausblendung ist der Wert im Fenster
    /// kurzzeitig falsch; dann gilt der zuletzt befohlene Zustand.</summary>
    public bool SponsorOn(bool observed) =>
        Environment.TickCount64 < _sponsorAssumedTil ? _sponsorAssumed : observed;

    /// <summary>Wird gerufen, wenn sich der Live-Zustand geändert haben könnte —
    /// damit das Deck sofort umfärbt statt erst beim nächsten Takt.</summary>
    public Func<Task>? StateChanged { get; set; }

    /// <summary>Wird mit der Beschriftung der Kopfzeile gerufen, sobald ein
    /// Modus scharf gestellt ist ("Tor", "Spieler", "Strafe 2'").</summary>
    public Func<string, Task>? ModeLabelChanged { get; set; }

    // ── Öffentlicher Einstieg ────────────────────────────────────────────────
    /// <summary>Führt einen Befehl der Brücke aus. Rückgabe: Meldung fürs Log,
    /// null bei Erfolg.</summary>
    public async Task<string?> ExecuteAsync(string verb, string[] args)
    {
        await _gate.WaitAsync();
        try
        {
            var note = await RunAsync(verb, args);
            if (StateChanged is not null) await StateChanged();
            return note;
        }
        finally { _gate.Release(); }
    }

    async Task<string?> RunAsync(string verb, string[] args)
    {
        string Arg(int i) => args.Length > i ? args[i].Trim() : "";

        // Die Seite steht je nach Befehl an unterschiedlicher Stelle:
        // "player|home|37" hat sie an 1, "prepare|goal|home|2" an 2.
        static string Side(string s) =>
            s.Equals("away", StringComparison.OrdinalIgnoreCase) ? "away" : "home";

        switch (verb)
        {
            // ── Einblendungen ohne Spielerwahl ───────────────────────────────
            case "lt":
                return await SwitchTo(Arg(1));

            // ── Umschalter des Einblenders ───────────────────────────────────
            // Kein "toggleshowhide" an TCunihockey: der zählt seinen eigenen
            // Zustand, und wenn der von dem abweicht, was tatsächlich läuft,
            // schaltet er verkehrt herum. Hier entscheidet der gelesene Zustand.
            case "toggle":
                if (IsLive()) { udp.Send($"{P}lowerthird_hide"); return null; }
                udp.Send($"{P}lowerthird_show");
                return null;

            case "hide":
                udp.Send($"{P}lowerthird_hide");
                return null;

            // ── Spielerwahl vorbereiten ──────────────────────────────────────
            // Blendet aus und stellt den Modus scharf. Eingeblendet wird erst,
            // wenn der Spieler gewählt ist.
            case "prepare":
                return await Prepare(Arg(1), Side(Arg(2)), Arg(3));

            case "player":
                return await Player(Side(Arg(1)), Arg(2));

            // ── Eigentor ─────────────────────────────────────────────────────
            case "owngoal":
                return await OwnGoal(Side(Arg(1)));

            // ── Starting 6 ───────────────────────────────────────────────────
            // Setzt nur den Modus. Die Knöpfe auf der Starting-6-Seite zeigen
            // die Namen, ausgewählt wird nichts — TCunihockey führt die Reihe
            // selbst und blättert mit "Weiter" durch.
            case "s6":
                await Clear();
                udp.Send($"{P}lowerthird_{Side(Arg(1))}_starting6");
                return null;

            // "Weiter": beim ersten Druck einblenden (TCunihockey zeigt dann von
            // selbst den ersten Spieler), danach je einen weiter.
            //
            // Beim sechsten Namen ist Schluss. Ein weiteres "Weiter" zählt in
            // TCunihockey auf 7/6 und blendet aus — gemessen. Das war der
            // gemeldete Fehler; deshalb wird hier abgeriegelt.
            case "s6next":
            {
                if (!IsLive()) { udp.Send($"{P}lowerthird_show"); return null; }

                var stand = reader.Starting6();
                if (stand is (var gezeigt, var total) && total > 0 && gezeigt >= total)
                    return $"Starting Six ist beim letzten Namen ({gezeigt}/{total}) — nicht weitergeschaltet";

                udp.Send($"{P}lowerthird_next");
                return null;
            }

            // ── Über Klicks im Fenster, weil ohne UDP-Befehl ─────────────────
            case "card":     return await ViaUi($"TcuUi=card|{Arg(1)}");
            case "shortcut": return await ViaUi($"TcuUi=shortcut|{Arg(1)}");
            case "highlight": return await ViaUi("TcuUi=highlight_result");

            case "fault":    return await ViaUi($"TcuUi=message|{FaultText(Arg(1))}");

            // Zuschauer zählen: nur ins Feld schreiben, NICHT einblenden —
            // sonst ginge bei jedem Tastendruck etwas auf Sendung.
            case "spectators":
                if (Arg(1).Equals("show", StringComparison.OrdinalIgnoreCase))
                    return await ViaUi("TcuUi=spectators|show");
                return ui.Execute($"TcuUi=spectators|{Arg(1)}", game);

            // ── Werbung ──────────────────────────────────────────────────────
            // Automatik und Einblendung immer gemeinsam — so ist der Zustand am
            // Kontrollkästchen "Auto" ablesbar und der Knopf bleibt synchron.
            case "sponsor":
            {
                var an = reader.SponsorAuto() != true;
                udp.Send($"{P}sponsor_auto_{(an ? "on" : "off")}");
                await Task.Delay(80);
                udp.Send($"{P}sponsor_{(an ? "show" : "hide")}");

                // Während der Werbung ausgeblendet wird, ist die Schaltfläche
                // "Live ( * )" gesperrt — und genau daran erkennt der Leser die
                // Automatik. Der Knopf würde deshalb nach dem Ausschalten noch
                // rund drei Sekunden "an" zeigen. Gemessen: aus / ab 250 ms
                // fälschlich an / ab 3000 ms wieder aus. Solange gilt das, was
                // hier befohlen wurde.
                _sponsorAssumed    = an;
                _sponsorAssumedTil = Environment.TickCount64 + 3500;
                return null;
            }

            default:
                return $"Unbekannter Befehl: {verb}";
        }
    }

    // ── Bausteine ────────────────────────────────────────────────────────────

    bool IsLive() => reader.LowerThirdLive() ?? false;

    /// <summary>Blendet aus und wartet, bis TCunihockey wieder umschaltbar ist.
    /// Läuft nichts, kehrt die Methode sofort zurück — das ist der Normalfall
    /// und darf keine Verzögerung kosten.</summary>
    async Task Clear()
    {
        if (!IsLive()) return;

        udp.Send($"{P}lowerthird_hide");
        logger.LogUi($"ausblenden, {FadeMs} ms warten");
        await Task.Delay(FadeMs);
    }

    async Task<string?> SwitchTo(string mode)
    {
        var cmd = ModeCommand(mode);
        if (cmd is null) return $"Unbekannte Einblendung: {mode}";

        await Clear();
        udp.Send(cmd);
        await Task.Delay(ArmMs);
        udp.Send($"{P}lowerthird_show");
        return null;
    }

    /// <summary>Klick im Fenster mit vorherigem Ausblenden und anschliessendem
    /// Einblenden. Der Klick wird von TCunihockey auf dessen eigenem Thread
    /// abgearbeitet — deshalb liegt zwischen Klick und Einblenden eine Pause.
    /// </summary>
    async Task<string?> ViaUi(string command)
    {
        await Clear();
        var note = ui.Execute(command, game);
        if (note is not null) return note;

        if (TcuUi.NeedsShow(command))
        {
            await Task.Delay(ArmMs);
            udp.Send($"{P}lowerthird_show");
        }
        return null;
    }

    /// <summary>
    /// Stellt einen Modus scharf, der noch eine Spielernummer braucht.
    ///
    /// "Tor" ist der Sonderfall: der UDP-Befehlssatz kennt nur
    /// lowerthird_home_goal+1, also Modus UND Torzählung in einem Schritt. Der
    /// Stand stiege damit hoch, bevor der Spieler gewählt ist. Deshalb wird
    /// hier nur der Knopf "Tor" im Fenster gedrückt; das "+1" folgt in Player(),
    /// nach der Spielerwahl.
    /// </summary>
    async Task<string?> Prepare(string kind, string side, string arg)
    {
        await Clear();

        _pendingKind = kind.ToLowerInvariant();
        _pendingSide = side;

        // Kopfzeile der Spielerseiten mitschreiben: sie sagt dem Anwender, was
        // der nächste Spielerdruck bewirkt. Die Seiten dienen allen Modi, ohne
        // diesen Hinweis wäre nach dem Seitenwechsel nicht mehr erkennbar,
        // welcher gerade scharf ist.
        if (ModeLabelChanged is not null) await ModeLabelChanged(ModeLabel(_pendingKind, arg));

        switch (_pendingKind)
        {
            case "goal":
                return ui.Execute($"TcuUi=team|{side}|Tor", game);

            case "name":
                udp.Send($"{P}lowerthird_{side}_name");
                return null;

            case "best":
                udp.Send($"{P}lowerthird_{side}_bestplayer");
                return null;

            case "penalty":
                return ui.Execute($"TcuUi=penalty|{side}|{arg}", game);

            default:
                _pendingKind = "";
                return $"Unbekannte Vorbereitung: {kind}";
        }
    }

    /// <summary>
    /// Spieler gewählt: Nummer setzen, bei "Tor" den Stand erhöhen, dann
    /// einblenden.
    ///
    /// <paramref name="nr"/> ist entweder eine Spielernummer oder — so kommt es
    /// vom Deck — ein Platz im Raster als "#7". Der Platz wird hier gegen den
    /// aktuellen Kader aufgelöst. Nur dadurch bleibt die Companion-Config von
    /// der Mannschaft unabhängig: ein neuer Kader ändert die Beschriftung und
    /// die Zuordnung, nicht die Config.
    /// </summary>
    async Task<string?> Player(string side, string nr)
    {
        if (nr.Length == 0) return "Keine Spielernummer";

        if (nr[0] == '#')
        {
            if (!int.TryParse(nr[1..], out var platz)) return $"Ungültiger Platz: {nr}";
            var kader = side == "away" ? game.AwayPlayers : game.HomePlayers;
            if (platz < 0 || platz >= kader.Count)
                return $"Platz {platz + 1} ist im aktuellen Kader nicht belegt ({kader.Count} Spieler)";
            nr = kader[platz].Display;
        }

        // Ohne vorherige Vorbereitung wäre offen, was eingeblendet werden soll.
        // Der Modus steht dann noch von der letzten Bedienung in TCunihockey —
        // die Nummer allein zu setzen ist dann das Richtige.
        var kind = _pendingKind;
        var s    = kind.Length > 0 ? _pendingSide : side;

        udp.Send($"{P}lowerthird_{s}_player|{nr}");

        if (kind == "goal")
        {
            // Reihenfolge ist die Anforderung: erst der Spieler, dann +1.
            await Task.Delay(ArmMs);
            var note = ui.Execute($"TcuUi=team|{s}|+1", game);
            if (note is not null) logger.Log($"Torzählung: {note}", LogLevel.Warning);
        }

        await Task.Delay(ArmMs);
        udp.Send($"{P}lowerthird_show");

        _pendingKind = "";
        return null;
    }

    /// <summary>
    /// Eigentor: derselbe Ablauf wie ein Tor, nur dass statt einer
    /// Spielernummer der Menüeintrag "Eigentor" gewählt wird.
    ///
    /// Der Eintrag steht ausschliesslich im Kontextmenü der Spielernummer — es
    /// gibt dafür weder einen UDP-Befehl noch einen Knopf im Fenster.
    /// </summary>
    async Task<string?> OwnGoal(string side)
    {
        await Clear();

        var note = ui.Execute($"TcuUi=team|{side}|Tor", game);
        if (note is not null) return note;
        await Task.Delay(ArmMs);

        var ziel = reader.Handle(side == "away" ? TcuState.IdNumberAway : TcuState.IdNumberHome);
        note = TcuMenu.Pick(ziel, "Eigentor", logger);
        if (note is not null) return note;

        // Wie beim Tor: erst der Verursacher, dann der Stand, dann Sendung.
        await Task.Delay(ArmMs);
        note = ui.Execute($"TcuUi=team|{side}|+1", game);
        if (note is not null) logger.Log($"Torzählung: {note}", LogLevel.Warning);

        await Task.Delay(ArmMs);
        udp.Send($"{P}lowerthird_show");
        return null;
    }

    /// <summary>Beschriftung der Kopfzeile auf den Spielerseiten.</summary>
    static string ModeLabel(string kind, string arg) => kind switch
    {
        "goal"    => "Tor",
        "name"    => "Spieler",
        "best"    => "Best Player",
        "penalty" => arg switch
        {
            "2"     => "Strafe 2'",
            "22"    => "Strafe 2'+2'",
            "10"    => "Strafe 10'",
            "match" => "Matchstrafe",
            _       => "Strafe",
        },
        _ => "Spieler",
    };

    // ── Texte ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Die Störungstexte stehen hier und nicht in der Companion-Config: über
    /// die UDP-Verbindung würden Umlaute zerbrechen (das Modul sendet Latin-1),
    /// während TcuConsole den Text mit WM_SETTEXT als Unicode ins Feld schreibt.
    /// Über die Leitung geht deshalb nur die Kennung.
    ///
    /// Einzeilig: das Feld heisst TxtShortcut14Line1 und ist auf eine Zeile
    /// ausgelegt.
    /// </summary>
    public static string FaultText(string kind) => kind.ToLowerInvariant() switch
    {
        "bild" => "Entschuldigen Sie die Bildstörung! Wir arbeiten mit Hochdruck an der Lösung!",
        "ton"  => "Entschuldigen Sie die Tonstörung! Wir arbeiten mit Hochdruck an der Lösung!",
        _      => "Entschuldigen Sie die technische Störung! Wir arbeiten mit Hochdruck an der Lösung!",
    };

    static string? ModeCommand(string mode) => mode.ToLowerInvariant() switch
    {
        "opener"        => $"{P}lowerthird_opener",
        "result"        => $"{P}lowerthird_result",
        "commentary"    => $"{P}lowerthird_commentary12",
        "referee"       => $"{P}lowerthird_referee12",
        "coach_home"    => $"{P}lowerthird_home_coach",
        "coach_away"    => $"{P}lowerthird_away_coach",
        "timeout_home"  => $"{P}lowerthird_home_timeout",
        "timeout_away"  => $"{P}lowerthird_away_timeout",
        "lineup_home"   => $"{P}lowerthird_home_lineup",
        "lineup_away"   => $"{P}lowerthird_away_lineup",
        _               => null,
    };
}
