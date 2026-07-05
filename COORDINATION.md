# Session-Koordination v2 (Worktrees + Branches)

> Die alte claim-basierte Koordination (ein gemeinsamer Working Tree) ist abgelöst.
> Historie: Git-Historie dieser Datei. **Ab jetzt arbeitet jede Session in einem eigenen
> Git-Worktree auf einem eigenen Branch** und muss nicht mehr eingeschränkt arbeiten.

## Arbeitsmodell

1. **Worktree + Branch anlegen** (einmalig pro Session, vom lokalen `main` aus):
   ```
   git worktree add C:\development\RedAnts-s<N> -b <branch>   # N = 1..5, Branch siehe Tabelle
   ```
   Danach ausschliesslich im eigenen Worktree arbeiten (`C:\development\RedAnts-s<N>`).
2. **Frei arbeiten**: keine Datei-Claims mehr, jede Session darf jede Datei ihres Pakets ändern.
   Committen wie gewohnt sofort und atomar; `git add -A` ist im eigenen Worktree wieder erlaubt.
3. **Schema-/Contract-Änderungen** (Tabellen, Enums, Port-Signaturen) weiterhin unten im
   Änderungs-Log ankündigen, damit die anderen beim Rebase wissen, was kommt.
4. **Merge nach `main`**: erst `git fetch` + Rebase auf aktuellen `main`, Release-Build grün
   (`dotnet build -c Release`), dann Merge und Push. `main` muss **immer grün** bleiben
   (CI baut vor jedem Deploy; roter `main` blockiert alle Deploys).
5. **Deploy**: Push `feature/**` → DEV, Push `main` → DEV + PROD (`.github/workflows/deploy.yml`).
6. **Merge-Reihenfolge / Abhängigkeiten**: S2 liefert das Tabellen-Fundament (Standard-Spalten,
   Confirm-Klick, Scanlog-Overlay, Erstellt-Spalte, `CreatedBy*`-Schema) **zuerst** und merged
   früh nach `main`; S1 (Freieinlass-Tab) und S4 (Saisonkarten-Tabelle) bauen darauf auf und
   rebasen danach. Alles andere ist unabhängig und kann sofort starten.
7. Regeln unverändert: keine Kommentare im Code (Rationale → `ARCHITECTURE.md`), Enums als int
   persistieren, Aliase aus den `*Aliases`-Klassen, keine Secrets im Code, kein Co-Authored-By.
8. **Statusupdates**: nur diese Datei darf direkt auf `main` committet werden (kleine
   Status-Commits aus `C:\development\RedAnts`); Code nur auf dem eigenen Branch.

## Aufgabenpakete

| Session | Branch | Worktree | Paket | Status |
|---|---|---|---|---|
| S1 | `feature/s1-scanner-freieinlass` | `C:\development\RedAnts-s1` | Scanner-Umbau + neuer Admin-Tab „Freier Einlass" + Umbenennung | **Paket komplett erledigt, in main gemerged**: Scanner (1–9) + Umbenennung (11) in `4c915c4`, Admin-Tab „Freier Einlass" (10) auf S2-Fundament gebaut |
| S2 | `feature/s2-admin-table-standard` | `C:\development\RedAnts-s2` | Tabellen-Standard (Fundament) + Spieltickets + Flextickets + Content-Links | **Paket komplett erledigt, in main gemerged**: Fundament `fe73334`, Spieltickets+Flextickets auf Standard + Content-Erstell-Links `cd7faea` (Release-Build grün) |
| S3 | `feature/s3-onlinekarten-mail` | `C:\development\RedAnts-s3` | Online-Karten-Design in Mails + Mailversand komplett + Kategorien mit Alter | **Erledigt, in main gemerged**: Mail-Karten in Online-Optik (Typ-Akzente, Logo im Kopf), QR via `/ticket/{token}/qr.png` (Gmail blockt data-URIs), Karten-Körper-Logo raus (Block4-Logo im Titelbalken), Kategorien mit Alter, Gate-Ausnahmen für `/ticket` + statische Assets (auf DEV verifiziert: Ticket 200/404 ohne Cookie, Shop bleibt 302). Azure: `Tickets__QrSecret` + Brevo auf dev+prod. **Achtung: alte Ticket-Links/QRs ungültig** (neuer Secret). |
| S4 | `feature/s4-kaufen-saisonkarten` | `C:\development\RedAnts-s4` | Kaufen/Warenkorb/Kasse + Saisonkarten-Admin (Bundles etc.) | offen |
| S5 | `feature/s5-ticketing-startseite` | `C:\development\RedAnts-s5` | Neue öffentliche Struktur (/ticketing/, /saisons/) + Seeder-Bereinigung | **Paket komplett erledigt, in main gemerged** (`d89f075`); lokal verifiziert (/, /ticketing/, /saisons/, Event-/Saisonseite je 200 inkl. Zurück-Button) |
| S6 | `feature/s6-admin-bestellungen` | `C:\development\RedAnts-s6` | Admin-Bereich „Bestellungen" (Onlineeinkäufe pro Saison) | **Paket erledigt, in main gemerged** (`886e0ee`, Release-Build grün, App-Boot + Route auf 54399 verifiziert): read-only Tab mit Saison-Dropdown, Spalten Kaufdatum, Adresse, Spieltickets, Saisonkarten, Betrag, Bezahlt-Status; alle Bestellungen inkl. „Unbezahlt". Buchhaltungs-Export + Zahlungs-Referenz offen (siehe Änderungs-Log) |

---

### S1 — Scanner + Freieinlässe (`feature/s1-scanner-freieinlass`)

Scanner (`Features/Ticketing/Scanning/`, `wwwroot/js/ticket-scanner.js`, `IAdmissionService`/`AdmissionService`):
1. Grünes Scan-Resultat wechselt NICHT mehr automatisch nach N Sekunden zurück, sondern erst
   nach Klick-Bestätigung.
2. Prägnantere Töne: OK = **1** Ton, Fehler = **2** Töne.
3. Nach einem Check-out wechselt der Modus automatisch zurück auf Check-in.
4. „Freier Einlass" gewähren UND zurücknehmen erfordern eine Klick-Bestätigung (Nachfrage).
5. Titel zeigt aktuell noch `_selected.Name` (Bug): dort den Anlassnamen anzeigen.
6. Button „Zurück" heisst neu „Event-Auswahl".
7. Von der Event-Auswahl aus: „Ausloggen" führt zurück zur Eingabe der scannenden Person.
8. Freie Einlässe können nur ausgecheckt werden, wenn tatsächlich freie Einlässe drin sind
   (andere Ticketarten zählen NICHT dazu). Anzeige, wie viele freie Einlässe aktuell drin sind.
9. QR-Fallback: Code kann per Tastatur eingegeben werden (die 8 Zeichen der Ticket-Kurzkennung).

Neuer Admin-Tab „Freier Einlass" (nach S2s Fundament):
10. Dropdowns Saison + Event; Liste der Freieinlässe. **Freieinlässe erhalten neu eine UUID**
    (Schema: neue Spalte, additiv). Spalten: Nr. (UUID-Anfang, gross), Erstellt (Standard-Spalte).

Umbenennung:
11. „Berechtigte" heisst überall neu **„Freier Einlass"** (Anlässe-Tabelle, Scanner, Labels).

### S2 — Admin-Tabellen-Standard + Spieltickets + Flextickets (`feature/s2-admin-table-standard`)

**Fundament zuerst bauen und früh nach `main` mergen** (S1 + S4 hängen davon ab):

Einheitlicher Tabellen-Standard für ALLE Ticket-Tabellen (Spaltenreihenfolge):
1. **Karte/Ticket-Nr.**: erste 8 Zeichen der UUID in GROSSBUCHSTABEN; direkt daneben das
   QR-Icon (Online-Ticket-Link).
2. **Käufer:in** (bei kaufbaren) bzw. **Inhaber:in** („—" falls leer; Geburtsdatum in Klammern
   falls vorhanden).
3. **Erstellt**: `dd.MM.yy / Initialen`; Tooltip = `dd.MM.yyyy HH:mm` + E-Mail der erstellenden
   Person. Initialen automatisch aus dem Namen. Bei Onlinekauf steht statt Initialen **„online"**.
   → Schema: additive Spalten `CreatedByName`/`CreatedByEmail` auf den Ticket-Tabellen
   (bei vorhandener `OrderId` gilt „online").
4. **Referenz** (Bundle bei Saisonkarten und Mitgliedern; bei Flextickets NICHT angezeigt).
5. **Kategorie** (Ticket- oder Mitgliederkategorie).
6. **Preis** (falls vorhanden).
7. **Status** (Gültig/Storniert): per Klick änderbar, mit Nachfragedialog. (Flexticket-Sperren
   🔒/🔓 = `TicketStatus.Blocked` bleibt als separates Icon bestehen.)
8. **Eingelöst** (Offen/Eingelöst, falls zutreffend): per Klick änderbar, mit Nachfragedialog.
9. **Eventbesuche**: Link öffnet Overlay mit Besuchen inkl. Scanlogs (Scannperson, wann ein-
   und ausgescannt; Daten aus `TicketEventVisits`/`TicketEventVisitsLogs`).
10. **Bearbeiten-Icon**. Icon-Abstände überall mindestens eine halbe Iconbreite.

Wiederverwendbare Bausteine dafür: Confirm-Klick-Muster, Scanlog-/Besuche-Overlay-Komponente,
Erstellt-Zelle (Initialen + Tooltip), zentrale CSS-Anpassungen in `ticketing-admin.css`.
ESC schliesst Dialoge weiterhin wie Abbrechen (globaler Handler in `Admin.cshtml`).

Dann anwenden:
- **Spieltickets**: Spalte „Nr." entfernen; Ticket-ID (= neue Ticket-Nr.) in Spalte 1; Scanlogs
  anzeigbar; Status UND Eingelöst per Klick + Nachfrage.
- **Flextickets**: dito; Status und Eingelöst als **zwei separate Icons** mit Nachfrage.
- **Admin-Links auf Content**: im Saisons-Tab ein Link ins Content zum Saison-Erstellen, im
  Anlässe-Tab ein Link ins Content zum Anlass-Erstellen.

### S3 — Online-Karten + Mail (`feature/s3-onlinekarten-mail`)

- **Mail-Ticketdesign** an die Online-Karten angleichen (gleiche Optik wie `WebTicket.cshtml`,
  inkl. Typ-Akzentfarben).
- **Logo nur noch im Titelbalken** der Online-Karte (kein grosses Logo im Kartenkörper mehr);
  bei Block4 steht im Titelbalken „Block 4" samt Block4-Logo, sonst Red-Ants-Logo.
- **Mailversand komplett implementieren** (Kaufbestätigung mit Tickets/QR; noch ohne Google
  Wallet). Absender: `tickets@redants.ch`.
- **Ticketkategorien mit Altersangabe**: Kind = „Kind (bis 6)", Jugend = „Jugend (bis 16)"
  (`TicketCategoryExtensions.DisplayName()`; wirkt überall, wo Kategorien angezeigt werden).

### S4 — Kaufen / Warenkorb / Kasse + Saisonkarten-Admin (`feature/s4-kaufen-saisonkarten`)

Kaufen:
- Warenkorb ist im Mobile-Modus ohne Aufklappen sichtbar; nach „In den Warenkorb" erscheint
  zusätzlich ein Button „Weiter zum Warenkorb".
- Beim Kaufabschluss werden **alle Kontingente nochmals geprüft** (Race-sicher; keine
  Überschreitung möglich).
- Öffentliche Anlassseite: Infobereich für den **Ort inkl. Google-Maps-Karte**.
- **Datum, Uhrzeit und Ort prominenter** darstellen und aus dem Header herausnehmen.

Warenkorb/Kasse:
- Anzahl wird sofort übernommen (ohne OK-Klick).
- **Rechnung und Barzahlung stehen nicht zur Verfügung**, auch nicht im Testmodus.

Saisonkarten-Admin (nach S2s Fundament):
- Karten-Nr. als erste Spalte (Standard).
- Eventbesuche als Overlay-Dialog anzeigbar, inkl. Scanlogs (Scannperson, ein-/ausgescannt wann).
- Beim Bearbeiten: Firma bzw. Vorname/Nachname der Käufer:in editierbar.
- Saisonkarten auch **im Bundle** erstellbar (Anzahl, Kategorie, Referenz, Abgabepreis, wie
  Flextickets); Bundle-Referenz als Spalte.
- Bundles als **CSV herunterladbar**: Button → Dialog zur Bundle-Auswahl → Download.

### S5 — Öffentliche Struktur + Seeder (`feature/s5-ticketing-startseite`)

- Startseite `/` leitet auf **`/ticketing/`** weiter; das ist die neue Startseite.
  Template mit Properties: **Headertext** und **Headerbild**. Inhalt: verkaufbare Saisonkarten
  (sofern vorhanden) und die kaufbaren Tickets (ohne Unterteilung nach Saison).
- **`/saisons/`**: Werbeseite für Saisonkarten inkl. Kaufbutton; konfigurierbar mit Header,
  Headerbild und WYSIWYG-Text.
- Unterseiten haben jeweils einen Button **„Zurück zur Hauptübersicht"**.
- **Seeder**: erstellt keine Beispielanlässe, -tickets, -venues oder -saisons mehr
  (Content-Types/Datentypen weiterhin code-first anlegen, nur keine Beispieldaten).

---

## Änderungs-Log (Schema/Contracts, die andere betreffen)

Neueste zuerst. Nur Änderungen eintragen, die andere Sessions betreffen.

| Datum | Session | Was | Auswirkung |
|---|---|---|---|
| 05.07.2026 | S6 | **Admin-Tab „Bestellungen" (Branch `feature/s6-admin-bestellungen`, auf `main` rebased):** neuer Tab-Key `orders` (`AdminOrdersComponent`) + Port `IOrderAdminReport`/`OrderListItem` + Reader `OrderAdminReportReader` (joint `Orders` mit `EventTickets`/`SeasonPasses`, ordnet Items der Saison via Event-Ids bzw. `SeasonId` zu). **Kein Schema geändert** (nutzt bestehende `Orders.Status`/`PaidAt`; alle Bestellungen werden gelistet, `Draft` = „Unbezahlt"). **Hinweis Bezahlstatus:** der Checkout ruft aktuell noch `order.MarkPaid()` sofort auf (`CheckoutController.Pay`), d.h. echte „Unbezahlt"-Zeilen entstehen erst, wenn die asynchrone Payrexx-Abwicklung die Bestellung als `Draft` anlegt und erst per Provider-Callback auf `Paid` setzt. | **S4 (Kasse/Payrexx)**: Beim Bau der asynchronen Zahlung die Bestellung als `Draft` speichern und erst nach Provider-Bestätigung `MarkPaid()` aufrufen; die Bestellungen-Übersicht zeigt Paid/Unbezahlt dann automatisch. Wer einen neuen Admin-Tab hinzufügt: `Tabs`-Array + `switch` in `TicketingAdminComponent.razor` sind die Konfliktstelle (additiv auflösen). |
| 05.07.2026 | S1 | **Schema: `TicketEventVisits.Uuid`** (additiv, NVARCHAR(36) NULL, Migrationsschritt `freeentry-uuid`). Freieinlässe erhalten beim Gewähren eine eigene UUID (`AdmissionService.GrantFreeEntryAsync`); Alt-Zeilen bleiben NULL (Anzeige „—"). Neuer Admin-Tab „Freier Einlass" (`AdminFreeEntriesComponent`, Tab-Key `freeentries`) + Port `IFreeEntryAdminReport`; Erstellt-Zelle via S2s `AdminFormat`, Erstellerin aus dem CheckIn-Scanlog. **In main gemerged.** | Wer `EventVisitRecord` schreibt/liest: neue nullable Property `Uuid`. Kein bestehender Contract geändert. |
| 05.07.2026 | S5 | **Öffentliche Struktur (auf Branch, Merge folgt):** (1) `/` redirectet auf `/ticketing/` (scan-Host weiter auf `/scanntickets`); die flexPage-„Startseite" wird an `/` nicht mehr ausgeliefert. (2) `ticketingRoot` hat neu Properties `headerText`/`headerImage` + Template `TicketingHome` (Node „Ticketing" ist jetzt öffentlich = neue Startseite: verkaufbare Saisonkarten + kaufbare Tickets ohne Saison-Gruppierung). (3) Neuer Doctype `saisonsPromo` (Header/Headerbild/WYSIWYG, Root-Node „Saisons" → `/saisons/` Werbeseite, Template `SaisonsPromo`). (4) Neuer Port `ISeasonPassPricing.GetAvailableAsync(seasonId)` (Verkaufbarkeit Saisonkarten aus `SeasonPrices` + verkauften gültigen `SeasonPasses`). (5) **Seeder erstellen keine Beispieldaten mehr** (`SampleCardsSeeder`/`PricingCatalogSeeder` gelöscht, Ticketing-Sample-Content raus; Struktur Root+Ordner bleibt). (6) `TicketEvent`/`TicketSeason`: Zurück-Button heisst „Zurück zur Hauptübersicht" und führt auf `/ticketing/`. | **Alle**: Frische Installationen starten ohne Demo-Daten (Dev-DB behält Bestand). **S4**: `TicketEvent.cshtml` von mir angefasst (nur `backHref` + Linktext) — beim Rebase beachten. **S3**: Kategorien-Labels erscheinen auch auf `/ticketing/`//`saisons/` via `DisplayName()`. Kaufbutton `/saisons/` verlinkt vorerst auf `/ticketing/#saisonkarten`; ein echter Saisonkarten-Kaufflow existiert noch nicht (S4-Kasse ist Event-Ticket-only). |
| 05.07.2026 | S2 | **Fundament in `main` (`fe73334`).** (1) Schema: nullbare Spalte `CreatedByEmail` (200) auf `EventTickets`/`SeasonPasses`/`MembershipCards`/`FlexTicketBundles` (Migrationsschritt `createdby-email-columns`); Domäne/Repos/Ports führen `createdByEmail` als optionalen Parameter neben `createdByName`. (2) `AdminIdentity(Name, Email, Initials)` wird vom Admin-Root als **CascadingParameter** an alle Tabs gereicht (`TicketingAdminController` liest die Backoffice-E-Mail aus den Claims). (3) Bausteine: `ConfirmDialog.razor` (Klick-Aktionen mit Nachfrage), `VisitsOverlay.razor` + `IVisitLogReader` (Eventbesuche inkl. Scanlogs per Ticket-Uuid), `AdminFormat` (`TicketNo` = 8 Zeichen UPPER, `CreatedShort`/`CreatedTooltip` mit Initialen bzw. „online"), zentrale CSS-Klassen `ta-ticketno`/`ta-badge-btn`/`ta-visits-link`/`ta-subtable`/`ta-visit*`. | **S1 (Freieinlass-Tab) + S4 (Saisonkarten-Tabelle)**: auf `main` rebasen und die Bausteine nutzen — `[CascadingParameter] AdminIdentity`, `AdminFormat.CreatedShort/CreatedTooltip`, `<ConfirmDialog>`, `<VisitsOverlay Uuid=... Title=... OnClose=...>`. Beim Erstellen von Tickets/Karten `createdByName: Identity.Name, createdByEmail: Identity.Email` durchreichen. |
| 05.07.2026 | S3 | **Auf Branch `feature/s3-onlinekarten-mail`** (Merge folgt): (1) `IQrCodeRenderer` hat neu `byte[] RenderPng(content, ppm)`; neuer Endpoint `GET /ticket/{token}/qr.png`. (2) `TicketCategory.DisplayName()`: Jugend → „Jugend (bis 16)", Kind → „Kind (bis 6)" (wirkt überall). (3) `WebTicketViewModel` ohne `MemberLogoUrl` (Logo nur noch im Titelbalken). (4) Azure dev+prod: `Tickets__QrSecret` gesetzt → **bisher erzeugte Ticket-Links/QRs sind ungültig** (Fallback-Key abgelöst); neue Links über `/ticket/for/{uuid}` erzeugen. | Wer `IQrCodeRenderer` mockt/implementiert: neue Methode. Kategorie-Labels ändern sich in allen Tabellen/Karten/Mails automatisch. Alte QR-Ausdrucke aus der Testphase neu erzeugen. |
| 05.07.2026 | S1 | **Scanner-Contract erweitert** (in main): `Occupancy` hat neu `int FreeInside = 0` (Anzahl freie Einlässe drinnen); `IAdmissionService` hat neu `ScanCodeAsync(eventId, shortCode, mode, scannedBy)` (löst die 8-Zeichen-Ticket-Nr. über alle vier Ticket-Tabellen per `Uuid LIKE 'prefix%'` auf). `TicketType.FreeEntry.DisplayName()` heisst neu „Freier Einlass" (vorher „Berechtigte"). Kein Schema geändert. | Wer `Occupancy` konstruiert oder `IAdmissionService` implementiert/mockt, den neuen Member beachten (Default-Parameter → meist quellkompatibel). Anzeigen mit „Berechtigte" übernehmen das neue Label automatisch via `DisplayName()`. |
| 05.07.2026 | S1 | Arbeitsmodell v2: Worktrees/Branches, Aufgabenpakete verteilt (dieses Dokument). | Alle: eigenen Worktree anlegen, Paket abarbeiten, früh mergen. S2s Fundament kommt zuerst. |
