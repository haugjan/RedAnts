# Session-Koordination v2 (Worktrees + Branches) — Runde 2

> Jede Session arbeitet in einem eigenen Git-Worktree auf einem eigenen Branch.
> Runde 1 (Scanner, Tabellen-Standard, Mail, Kasse, /ticketing/, Bestellungen) ist komplett
> in `main` gemerged und deployed. Dieses Dokument beschreibt die **Runde-2-Pakete**.

## Arbeitsmodell

1. **Runde-2-Branch im bestehenden Worktree anlegen** (einmalig pro Session):
   ```
   git -C C:\development\RedAnts-s<N> fetch origin
   git -C C:\development\RedAnts-s<N> checkout -b <branch> origin/main   # Branch siehe Tabelle
   ```
   Danach ausschliesslich im eigenen Worktree arbeiten (`C:\development\RedAnts-s<N>`).
2. **Frei arbeiten**: keine Datei-Claims; sofort und atomar committen, `git add -A` erlaubt.
3. **Schema-/Contract-Änderungen** (Tabellen, Enums, Port-Signaturen) im Änderungs-Log unten
   ankündigen, damit die anderen beim Rebase wissen, was kommt.
4. **Merge nach `main`**: `git fetch` + Rebase auf aktuellen `main`, Release-Build grün
   (`dotnet build -c Release`), dann Merge und Push. `main` muss **immer grün** bleiben.
5. **Deploy**: Push `feature/**` → DEV, Push `main` → DEV + PROD.
6. **Merge-Reihenfolge**: S2 liefert das **Inline-Edit-Fundament** (Tabellenzellen direkt
   editierbar mit Nachfragedialog) zuerst und merged früh; S4/S5/S6 bauen ihre Inline-Edits
   darauf. S5 liefert das neue Saisons-Preismodell (Sonderaktion/Verfügbarkeit) als Port,
   den S4 für den öffentlichen Saisonkarten-Kauf konsumiert. S1/S3 sind unabhängig.
7. Regeln: keine Kommentare im Code (Rationale → `ARCHITECTURE.md`), Enums als int, Aliase aus
   `*Aliases`, keine Secrets im Code, kein Co-Authored-By.
8. **Statusupdates**: nur diese Datei direkt auf `main` committen; Code nur auf dem Branch.

## Aufgabenpakete Runde 2

| Session | Branch | Worktree | Paket | Status |
|---|---|---|---|---|
| S1 | `feature/s1-r2-freieinlass-scan` | `C:\development\RedAnts-s1` | Freier Einlass (Kategorien/Kontingente) + Scan-App-Layout | offen |
| S2 | `feature/s2-r2-inline-edit` | `C:\development\RedAnts-s2` | Inline-Edit-Fundament (zuerst!) + Spieltickets + Flextickets | offen |
| S3 | `feature/s3-r2-google-wallet` | `C:\development\RedAnts-s3` | Google Wallet | offen |
| S4 | `feature/s4-r2-verkauf-saisonkarten` | `C:\development\RedAnts-s4` | Verkauf-Fixes + öffentlicher Saisonkarten-Kauf + Saisonkarten-Admin | offen |
| S5 | `feature/s5-r2-saisons-admin` | `C:\development\RedAnts-s5` | Saisons-Admin komplett (inkl. neues Preismodell + Sonderaktion) | offen |
| S6 | `feature/s6-r2-anlaesse-mitglieder` | `C:\development\RedAnts-s6` | Anlässe-Admin + Mitgliederkarten | offen |

---

### S1 — Freier Einlass + Scan-App (`feature/s1-r2-freieinlass-scan`)

Freier Einlass:
1. Beim Gewähren wird die **Kategorie** unterschieden: Funktionär oder Swiss-Unihockey-
   Freieintritt (bestehendes Enum `FreeEntryType` nutzen/anpassen); Kategorie-Wahl im Scanner.
2. Die Kategorie wird in der Freier-Einlass-Tabelle als Spalte angezeigt.
3. Freie Einlässe **zählen zum Einlasskontingent** des Anlasses (verifizieren, gilt heute für
   die Occupancy; sicherstellen, dass alle Anzeigen konsistent sind).
4. **SU-Freieintritt-Kontingent pro Anlass** wählbar (neues Feld, z. B. auf `EventPrices`);
   SU-Freieintritte dürfen dieses Kontingent **nicht überschreiten** (Scanner lehnt ab).
5. Check-out-Regel: es dürfen nur so viele Personen per Freieintritt auschecken, wie freie
   Eintritte drin sind (pro Kategorie korrekt zählen).
6. UUID der Freieinlässe in der Tabelle anzeigen, analog den anderen Eintrittsarten
   (Kurzkennung, gross; existiert, verifizieren/vereinheitlichen).
7. Menüpunkt „Freier Einlass" neu **direkt nach Flextickets** (Tab-Reihenfolge).

Scan-App:
8. Das gesamte Scan-UI passt auf eine übliche Handyseite **ohne Scrollen** (Layout verdichten:
   Kamera, Kapazität, Buttons, manuelle Eingabe).

### S2 — Inline-Edit-Fundament + Spieltickets + Flextickets (`feature/s2-r2-inline-edit`)

**Fundament zuerst bauen und früh mergen** (S4/S5/S6 hängen davon ab):
- Wiederverwendbare **Inline-Edit-Zellen mit Nachfragedialog** für Tabellen: Text, Zahl,
  Datum, Zeit und Select/Badge. Verhalten: Wert direkt in der Zelle ändern → Dialog „…wirklich
  ändern?" (alt → neu) → bestätigen speichert, abbrechen setzt zurück. Baut auf
  `ConfirmDialog` auf; zentrale CSS in `ticketing-admin.css`.

Spieltickets:
- Lead-Text „Verkaufte Spieltickets eines Anlasses …" entfernen.
- **Kategorie** und **Abgabepreis** direkt in der Tabelle editierbar (mit Nachfrage).
- Admin-Änderungen an „Eingelöst" werden **im Scanlog getrackt** (wie ein Eingangsscan, mit
  Admin-Identity als Scannperson). Neue Zustände: **Offen / Eingelöst / Draussen**
  (Draussen = war drin und ist ausgecheckt; aus `TicketEventVisits.IsInside` ableiten).

Flextickets:
- **Kategorie** direkt in der Tabelle editierbar (mit Nachfrage).
- **BUG: Klick auf Status crasht die Applikation** → beheben (Fehlerbild reproduzieren,
  vermutlich Statuswechsel-Handler).
- Schlösschen-Icons (🔒/🔓) rechts entfernen; der Status wird direkt am Ticket gesetzt.
- Admin-Änderungen an „Eingelöst" ebenfalls im Scanlog tracken (wie beim Eingangsscan).

### S3 — Google Wallet (`feature/s3-r2-google-wallet`)

- „Zu Google Wallet hinzufügen"-Button auf der Web-Ticket-Seite und in der Ticket-Mail.
- Implementierung: Google Wallet API (EventTicket-/Generic-Pass-Klasse pro Tickettyp),
  signierte „Save to Wallet"-JWTs über einen Google-Service-Account; Pass-Inhalte analog
  Online-Karte (Typ, Kategorie, Anlass/Saison, Ticket-Nr., QR mit `/ticket/{token}`-URL).
- Secrets über Konfiguration (`GoogleWallet:IssuerId`, Service-Account-Key als Secret/
  App-Setting), niemals im Code.
- **Vom Nutzer benötigt** (blockierend): siehe Abschnitt „Google Wallet: benötigte Zugänge"
  im Chat/Änderungs-Log; bis dahin gegen Demo-Issuer implementieren und Button hinter
  Config-Flag lassen.

### S4 — Verkauf + Saisonkarten-Kauf + Saisonkarten-Admin (`feature/s4-r2-verkauf-saisonkarten`)

Verkauf:
- **Warenkorb-Seite ist zu breit fürs Handy** → mobil fixen.
- Anlassseite: zuerst die **Ticketkategorien zum Kaufen**, danach erst die Google-Maps-Karte.
- **Wochentage lokalisiert**: bei deutschsprachigem Browser deutsch (Request-Localization/
  Accept-Language statt fixer Server-Culture für die Anzeige).
- **Captcha live schalten**: Turnstile-Code ist fertig; auf Prod fehlen die Cloudflare-Keys
  (`Turnstile__SiteKey`/`__SecretKey` als App-Settings). Sobald der Nutzer die Keys liefert,
  setzen und Ende-zu-Ende testen.

**Öffentlicher Saisonkarten-Kauf (NEU, vom Nutzer):** Saisonkarten können aktuell öffentlich
nicht gekauft werden → Kaufflow bauen: von `/ticketing/` bzw. `/saisons/` in den Warenkorb,
Checkout erstellt `SeasonPass` + Order (analog Spieltickets), Mail mit Karte. Verfügbarkeit
über S5s Preismodell-Port (Sonderaktion-Regel); bis S5 merged, gegen bestehendes
`ISeasonPassPricing.GetAvailableAsync` bauen und danach andocken.

Saisonkarten-Admin:
- **Status** und **Kategorie** direkt in der Tabelle editierbar (S2-Fundament, mit Nachfrage).
- **CSV-Export analog Flextickets**: Button → Dialog mit Bundle-Auswahl → Download.

### S5 — Saisons-Admin komplett (`feature/s5-r2-saisons-admin`)

Tabelle:
- Lead-Text („Saisons verwalten: Status …") entfernen.
- „Saison im Content erstellen" öffnet **keinen neuen Tab**.
- **Verkaufsstatus** (umbenannt von „Status") direkt in der Tabelle editierbar (Nachfrage).
- **Saisonbezeichnung** und **Zeitraum** direkt in der Tabelle editierbar (Nachfrage).
- „Gesamtkontingent" heisst neu **„Einlasskontingent"**, direkt in der Tabelle editierbar
  (Nachfrage) und zeigt an, **wie viel bereits verbraucht** ist.
- Neue Spalten: **Anzahl Anlässe**; direkt nach dem Einlasskontingent: **verkaufte
  Saisonkarten, verkaufte Tickets, Anzahl Einlässe**.
- Nach Klick auf einen Link: sichtbares Feedback **„in Zwischenablage kopiert"**.
- Bearbeiten-Icon wird zum **Dollarsymbol** ($) — das Overlay enthält nur noch die
  Kategorien/Preise (alles andere ist inline).

Preismodell (Schema/Contract, im Log ankündigen):
- **Zwei Preise pro Kategorie**: Saisonkarten-Preis UND Standard-Ticketpreis.
- Pro Ticketkategorie ein **Boolean „angeboten"**.
- Kontingent pro Kategorie mit Anzeige, **wie viel bereits verbraucht**.
- **„Reduziert" wird durch „Sonderaktion" ersetzt** (Kategorien-Umbau); normale Saisonkarten
  sind erst verkäuflich, **wenn keine Sonderaktions-Tickets mehr verfügbar** sind.
- Verfügbarkeits-Port für S4s Kaufflow bereitstellen/aktualisieren
  (`ISeasonPassPricing` erweitern).

### S6 — Anlässe-Admin + Mitgliederkarten (`feature/s6-r2-anlaesse-mitglieder`)

Anlässe:
- „Anlass im Content erstellen" öffnet **keinen neuen Tab**; Lead-Text entfernen.
- **„Zeit noch unbekannt"** pro Anlass wählbar → überall nur das Datum anzeigen (Content-Type
  + öffentliche Seiten + Admin + Scanner-Eventliste).
- Direkt in der Tabelle editierbar (S2-Fundament, je mit Nachfrage): **Bezeichnung,
  Zeitpunkt, Verkaufsstatus (umbenannt), Einlasskontingent**.

Mitgliederkarten:
- **Einzelkarte direkt erstellbar**: Name optional, Geburtsdatum optional, **Referenz
  Pflicht und eindeutig pro Saison**.
- **Karten-Nr. als erste Spalte inkl. QR-Link** (Tabellen-Standard); **Geburtsdatum in der
  Inhaber:in-Spalte** (Klammern); **Erstellt-Spalte** nach Standard (dd.MM.yy/Initialen +
  Tooltip); **Eventbesuche-Overlay** (wie Saisonkarten).
- **CSV-Export analog Flextickets** mit Auswahl des Bundles (= Referenz).

---

## Änderungs-Log (Schema/Contracts, die andere betreffen)

Neueste zuerst. Nur Änderungen eintragen, die andere Sessions betreffen.

| Datum | Session | Was | Auswirkung |
|---|---|---|---|
| 05.07.2026 | S1 | Runde-2-Plan verteilt (dieses Dokument). | Alle: Runde-2-Branch anlegen, Paket abarbeiten. S2s Inline-Edit-Fundament zuerst; S5s Preismodell vor S4s Saisonkarten-Kauf-Feinschliff. |
| 05.07.2026 | S6 | **Admin-Tab „Bestellungen" (Branch `feature/s6-admin-bestellungen`, auf `main` rebased):** neuer Tab-Key `orders` (`AdminOrdersComponent`) + Port `IOrderAdminReport`/`OrderListItem` + Reader `OrderAdminReportReader` (joint `Orders` mit `EventTickets`/`SeasonPasses`, ordnet Items der Saison via Event-Ids bzw. `SeasonId` zu). **Kein Schema geändert** (nutzt bestehende `Orders.Status`/`PaidAt`; alle Bestellungen werden gelistet, `Draft` = „Unbezahlt"). **Hinweis Bezahlstatus:** der Checkout ruft aktuell noch `order.MarkPaid()` sofort auf (`CheckoutController.Pay`), d.h. echte „Unbezahlt"-Zeilen entstehen erst, wenn die asynchrone Payrexx-Abwicklung die Bestellung als `Draft` anlegt und erst per Provider-Callback auf `Paid` setzt. | **S4 (Kasse/Payrexx)**: Beim Bau der asynchronen Zahlung die Bestellung als `Draft` speichern und erst nach Provider-Bestätigung `MarkPaid()` aufrufen; die Bestellungen-Übersicht zeigt Paid/Unbezahlt dann automatisch. Wer einen neuen Admin-Tab hinzufügt: `Tabs`-Array + `switch` in `TicketingAdminComponent.razor` sind die Konfliktstelle (additiv auflösen). |
| 05.07.2026 | S1 | **Schema: `TicketEventVisits.Uuid`** (additiv, NVARCHAR(36) NULL, Migrationsschritt `freeentry-uuid`). Freieinlässe erhalten beim Gewähren eine eigene UUID (`AdmissionService.GrantFreeEntryAsync`); Alt-Zeilen bleiben NULL (Anzeige „—"). Neuer Admin-Tab „Freier Einlass" (`AdminFreeEntriesComponent`, Tab-Key `freeentries`) + Port `IFreeEntryAdminReport`; Erstellt-Zelle via S2s `AdminFormat`, Erstellerin aus dem CheckIn-Scanlog. **In main gemerged.** | Wer `EventVisitRecord` schreibt/liest: neue nullable Property `Uuid`. Kein bestehender Contract geändert. |
| 05.07.2026 | S5 | **Öffentliche Struktur (in main):** (1) `/` redirectet auf `/ticketing/` (scan-Host weiter auf `/scanntickets`). (2) `ticketingRoot` mit `headerText`/`headerImage` + Template `TicketingHome` (= neue Startseite). (3) Doctype `saisonsPromo` → `/saisons/` Werbeseite. (4) Port `ISeasonPassPricing.GetAvailableAsync(seasonId)`. (5) Seeder ohne Beispieldaten. (6) Zurück-Buttons auf `/ticketing/`. | **Alle**: Frische Installationen ohne Demo-Daten. Kaufbutton `/saisons/` verlinkt auf `/ticketing/#saisonkarten`; echter Saisonkarten-Kaufflow ist Runde-2-Aufgabe von S4. |
| 05.07.2026 | S2 | **Tabellen-Fundament (in main):** `CreatedByName`/`CreatedByEmail`-Spalten, `AdminIdentity` als CascadingParameter, Bausteine `ConfirmDialog.razor`, `VisitsOverlay.razor` + `IVisitLogReader`, `AdminFormat` (TicketNo/Initials/CreatedShort/CreatedTooltip), zentrale CSS `ta-ticketno`/`ta-badge-btn`/`ta-visits-link`/`ta-subtable`. | Beim Erstellen von Tickets/Karten `createdByName: Identity.Name, createdByEmail: Identity.Email` durchreichen. |
| 05.07.2026 | S3 | **In main:** `IQrCodeRenderer.RenderPng` + `GET /ticket/{token}/qr.png`; Kategorie-Labels mit Alter; `Tickets__QrSecret` auf dev+prod gesetzt → alte Ticket-Links/QRs aus der Testphase sind ungültig, neu erzeugen. | Wer `IQrCodeRenderer` mockt: neue Methode. |
| 05.07.2026 | S1 | **Scanner-Contract (in main):** `Occupancy.FreeInside`, `IAdmissionService.ScanCodeAsync` (8-Zeichen-Kurzcode über alle vier Ticket-Tabellen), `FreeEntry.DisplayName()` = „Freier Einlass". | `Occupancy`-Konstruktion/Mocks: neuer Member mit Default. |
