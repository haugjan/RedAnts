# RedAnts Show

Die Buttons in Companion sind nur **Plätze (Slots 1–15)**. Was darauf steht (Text, Icon, Farbe) und was ein Druck auslöst, kommt live vom geöffneten Board: Slot 1–15 entspricht dem 5×3-Raster des Boards, Zeile für Zeile von links oben.

## Einrichten

1. **Server-URL**: `https://show.redants.ch` (prod) oder `https://show-dev.redants.ch` (dev).
2. **API-Key**: `Show:ApiKey`, ersatzweise das Board-Passwort.
3. **Board-Code (Room)**: optional. Bei mehreren gleichzeitig offenen Boards das Board mit `?room=CODE` öffnen und denselben Code hier eintragen. Leer = das zuletzt aktive Board.

Das Board (`/show`) muss im Browser offen und einmal angetippt sein (Ton-Freigabe), sonst spielt nichts.

## Buttons anlegen (einmalig)

- Preset-Kategorie **Slots**: `Slot 1` bis `Slot 15` auf die gewünschten Tasten ziehen. Jeder Slot hat die Aktion *Slot drücken* und die Anzeige *Slot-Anzeige* mit derselben Nummer.
- Preset-Kategorie **Steuerung**: Profil vor/zurück (zeigt das aktive Profil), Home, Zurück, Stopp, Pause, Weiter, Fade-out.

## Profile

Die Slots folgen immer dem Profil und Ordner, die das Board gerade zeigt. Ein Profilwechsel am Board oder über *Profil: nächstes/vorheriges* bzw. *Profil wählen* ändert sofort alle Slots.

## Räume

Jeder Board-Code (Room), den Companion sendet, erscheint oben im Board in der Raumauswahl (● = gerade aktiv). Dort den Raum wählen, das Board merkt ihn sich im Browser. `?room=CODE` in der Board-URL hat Vorrang. Im Show-Admin unter „Räume“ lassen sich Räume löschen; sie erscheinen wieder, sobald Companion sie erneut sendet.

## Variablen

`profile`, `path`, `now_playing`, `connected`, `unlocked`, `room`, `slot_1` … `slot_15`.
