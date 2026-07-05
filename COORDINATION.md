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
| S1 | `feature/s1-r2-freieinlass-scan` | `C:\development\RedAnts-s1` | Freier Einlass (Kategorien/Kontingente) + Scan-App-Layout | **erledigt, in main gemerged** |
| S2 | `feature/s2-r2-inline-edit` | `C:\development\RedAnts-s2` | Inline-Edit-Fundament (zuerst!) + Spieltickets + Flextickets | **Paket komplett erledigt, in main gemerged**: Fundament `51fc8b8`, Spieltickets+Flextickets `9563af7` (inkl. Status-Klick-Crash-Fix, Release-Build grün) |
| S3 | `feature/s3-r2-verkauf` | `C:\development\RedAnts-s3` | Verkauf-Fixes (Warenkorb mobil, Reihenfolge, Wochentag, Captcha) | **Erledigt, in main gemerged** (`ca832ba`): Warenkorb <576px als Karten-Stack; Kategorien vor Maps; Wochentag via `DisplayCulture.From(Request)` (Accept-Language; Umbraco überschreibt die Request-Culture mit der Content-Sprache, darum KEIN `UseRequestLocalization` — siehe ARCHITECTURE.md); Turnstile verifiziert, DEV mit Testkeys, **Prod-Keys gesetzt: Turnstile auf Prod scharf, E2E verifiziert (Widget rendert mit echtem SiteKey; Kauf ohne Token wird serverseitig abgelehnt, keine Order entsteht)**. Nebenbei: DEV-Start-Timeout auf 600s erhöht (Basic-DB 5 DTU bricht unter Parallel-Last Verbindungen ab — Upgrade auf S0 empfohlen, wartet auf Nutzer-OK) |
| S4 | `feature/s4-r2-saisonkarten` | `C:\development\RedAnts-s4` | Öffentlicher Saisonkarten-Kauf + Saisonkarten-Admin | **erledigt, in main gemerged (`1e413e1`)**: Kauf über `/ticketing/`, Admin Status/Kategorie inline (S2-Fundament), CSV-Export je Saison |
| S5 | `feature/s5-r2-saisons-admin` | `C:\development\RedAnts-s5` | Saisons-Admin komplett (inkl. neues Preismodell + Sonderaktion) | **Paket komplett erledigt, in main gemerged** (Preismodell `91776c7`, Tabelle/Inline `3a631f4`; Migration gegen Dev-DB verifiziert) |
| S6 | `feature/s6-r2-anlaesse-mitglieder` | `C:\development\RedAnts-s6` | Anlässe-Admin + Mitgliederkarten | **erledigt; Branch war komplett, von S1 nach main gemerged** (Inline-Edits Name/Zeitpunkt/Verkaufsstatus/Kontingent, „Zeit unbekannt", Mitgliederkarten inkl. Einzelkarte + CSV) |

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

### S3 — Verkauf-Fixes (`feature/s3-r2-verkauf`)

- **Warenkorb-Seite ist zu breit fürs Handy** → mobil fixen.
- Anlassseite: zuerst die **Ticketkategorien zum Kaufen**, danach erst die Google-Maps-Karte.
- **Wochentage lokalisiert**: bei deutschsprachigem Browser deutsch; englisch nur, wenn der
  Browser wirklich englisch ist (Request-Localization/Accept-Language für die Anzeige statt
  fixer Server-Culture).
- **Captcha live schalten**: Turnstile-Code ist fertig (Widget auf der Zahlungsseite +
  serverseitige Prüfung in `CheckoutController.Pay`); auf Prod fehlen nur die Cloudflare-Keys
  (`Turnstile__SiteKey`/`__SecretKey` als App-Settings). Ablauf verifizieren; sobald der
  Nutzer die Keys liefert, setzen und Ende-zu-Ende testen.

> **Google Wallet ist ZURÜCKGESTELLT** (Entscheid Nutzer). Kommt in einer späteren Runde,
> sobald Issuer-Konto/Service-Account vorliegen. Nicht beginnen.

### S4 — Saisonkarten-Kauf + Saisonkarten-Admin (`feature/s4-r2-saisonkarten`)

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

## Nachtrag Runde 2: Abendkasse-Express (vom Nutzer freigegeben)

Ziel: An der Abendkasse scannt jemand ein fixes QR-Plakat und kommt in wenigen Schritten zum
Ticket, ohne dass Bestell-Informationen verloren gehen.

**S4 — Express-Checkout** (`feature/s4-r2-abendkasse`):
1. **Fixe Route `/heute`**: löst den heutigen Anlass auf und leitet auf dessen Eventseite
   weiter (kein Event heute → nächste Events; mehrere → Auswahl). Ein nie neu zu druckendes
   QR-Plakat zeigt auf `https://tickets.redants.ch/heute`.
2. **„Direkt kaufen"** auf der Eventseite: Anzahl wählen → direkt in die Kasse (Warenkorb
   bleibt internes Modell, die Zwischenseite entfällt).
3. **Express-Kasse in einem Schritt**: nur E-Mail (Pflicht) + Zahlungsart + Turnstile;
   Rechnungsadresse optional. Bestellung/Beleg entstehen unverändert (Nummer, Betrag,
   Zahlungsart, E-Mail).
4. **Bestätigungsseite zeigt die gekauften Tickets mit QR direkt an** (Karten wie das
   Online-Ticket) — kein Warten aufs Mail an der Tür.

**S3 — Mail-Ticket-Layout** (`feature/s3-r2-mail-abstand`):
- Bei mehreren Tickets pro Mail: jede Karte deutlich abgesetzt (grosser Abstand/Trenner),
  Laufnummer „Ticket X von Y", Print-Umbrüche (`page-break-inside: avoid`), damit beim
  Ausdrucken nichts zusammenklebt.

**S1 — Gratis-Kind im Scanner** (`feature/s1-r2-kind-gratis`):
- Neuer `FreeEntryType.Child` („Kind (gratis)") als dritte Option im Freieinlass-Dialog
  (gewähren + zurücknehmen). Zählt zum Hallenkontingent, eigene Kategorie in Tabelle und
  Zahlen, verbraucht KEIN SU-Kontingent. Additiv (neuer int-Wert).

## Änderungs-Log (Schema/Contracts, die andere betreffen)

Neueste zuerst. Nur Änderungen eintragen, die andere Sessions betreffen.

| Datum | Session | Was | Auswirkung |
|---|---|---|---|
| 05.07.2026 | S1 | **Nachtrag Gratis-Kind in `main`:** `FreeEntryType` hat neu den Wert **`Child` (= int 4)**, Label „Kind (gratis)" via `DisplayName()`. Dritte Option im Freieinlass-Dialog des Scanners (gewähren + zurücknehmen); zählt zum Hallenkontingent, verbraucht kein SU-Kontingent, erscheint automatisch als Kategorie im Freier-Einlass-Tab. Kein Schema geändert (int-Spalte). | Wer über `FreeEntryType` `switch`t: neuen Fall beachten (Label via `DisplayName()`). |
| 05.07.2026 | S2 | **Spieltickets+Flextickets in `main` (`9563af7`), Paket komplett.** (1) **Contracts (additiv):** `FlexTicketView` neu mit `Category` + `IsInside`; `IFlexTicketBundles` neu mit `SetTicketCategoryAsync(uuid, category)`; `IVisitLogReader` neu mit `GetInsideByEventAsync(eventId)` → `Dictionary<Guid,bool>`; neues Enum `RedemptionState { Open, Redeemed, Outside }` + `Derive(redeemed, isInside)` in `VisitLog.cs`. (2) **Scanner-Fix:** `AdmissionService` setzt beim EventTicket-CheckIn jetzt auch `EventTickets.Redeemed = 1` (war nur bei SeasonSingle). (3) Admin-Einlösungsänderungen laufen über `IAdmissionService.ScanTicketAsync` → jede Änderung steht im Scanlog mit Admin-Identity; Flex-Einlösen fragt den Anlass ab. (4) **Status-Klick-Crash behoben**: Toggle-Pfade ersetzt durch Inline-Selects (`InlineEditBase` fängt alle Exceptions in den Dialog). | Wer `FlexTicketView` konstruiert: zwei neue optionale Felder. Wer `IFlexTicketBundles`/`IVisitLogReader` mockt: neue Methoden. **S4/S6**: für „Eingelöst"-Zellen `RedemptionState`/`Derive` wiederverwenden statt eigener Ableitung. |
| 05.07.2026 | S5 | **Saisons-Admin komplett in `main` (`3a631f4`).** Tabelle: Inline-Edits (S2-Bausteine) für Verkaufsstatus/Bezeichnung/Zeitraum/Einlasskontingent; neue Spalten Anlässe, verk. Saisonkarten, verk. Tickets, Einlässe (+ „x verbraucht" beim Kontingent); Clipboard-Feedback; $-Overlay nur noch Kategorien/Preise (Doppelpreis, Angeboten, Kontingent, Verbraucht). **Contracts (additiv):** `ISeasonStatusEditor` neu mit `SetNameAsync`/`SetPeriodAsync`; neuer Port `ISeasonStatsReader.GetAsync(seasonId, eventIds)` → `SeasonStats(PassesSold, TicketsSold, Admissions)`. | **S6** (Anlässe-Inline-Umbau): `UmbracoSeasonStatusEditor`/`SeasonStatsReader` als Vorlage für Event-Pendants nutzbar; `ISeasonStatsReader` liefert auch eure Saison-Zähler, falls gebraucht. Keine bestehende Signatur geändert. |
| 05.07.2026 | S6 | **Anlass „Zeit noch unbekannt" (Branch `feature/s6-r2-anlaesse-mitglieder`, folgt in `main`).** Neues Boolean-Content-Feld `timeUnknown` am `event`-Doctype (Alias `TicketingAliases.EventTimeUnknown`), idempotent auch auf bestehende Installationen nachgerüstet (`EnsureEventExtraProperties`). **Contract:** `Event.FromPersistence(...)` hat neu einen Parameter `bool timeUnknown` (direkt nach `startTime`, vor `venueId`); neue Property `Event.TimeUnknown`. Ist das Feld gesetzt, zeigen alle Datum+Zeit-Anzeigen (öffentliche Anlass-/Home-/Saison-Seite, Admin-Anlässe, Scanner-Eventliste, Web-Ticket, Bestell-Mail) nur das Datum. | Wer `Event.FromPersistence` aufruft oder `Event` konstruiert/mockt: neuer Parameter (nur der Umbraco-Reader ruft es real auf). Kein NPoco-DB-Schema, reines Umbraco-Content-Feld. |
| 05.07.2026 | S5 | **Neues Saisons-Preismodell in `main` (`91776c7`).** (1) **Enum-Umbau**: `TicketCategory.AdultReduced`→`AdultPromo` („Sonderaktion Erwachsen"), `YouthReduced`→`YouthPromo` („Sonderaktion Jugend") — nur Code-Namen/Labels, **Int-Werte unverändert** (1/3), bestehende Daten bleiben gültig. Neu `TicketCategoryExtensions.IsPromo()`/`PromoCounterpart()`. (2) **Schema** (additiv, Step `seasonprices-dual-price`): `SeasonPriceCategories.TicketPrice` (decimal null) + `.Offered` (bit null; null=angeboten für Alt-Zeilen). (3) **Domäne**: neues `SeasonCategoryPrice(Category, PassPrice, TicketPrice, Offered, Quota)`; `SeasonPrice.Categories` ist jetzt `IReadOnlyList<SeasonCategoryPrice>` (vorher `CategoryPrice`). (4) **Port `ISeasonPassPricing` erweitert**: `GetAvailableAsync` liefert nur angebotene Kategorien und wendet die **Sonderaktions-Regel** an (normale Kategorie nicht verfügbar, solange die zugehörige angebotene Sonderaktion Restkontingent hat); neu `CheckCapacityAsync(IReadOnlyList<PassDemand>)` (Race-Check analog `IEventPricing.CheckCapacityAsync`, Meldung oder null) und `GetSoldCountsAsync(seasonId)`. | **S4** (Saisonkarten-Kauf): euer Flow dockt via `GetAvailableAsync` automatisch an; bitte im Checkout zusätzlich `CheckCapacityAsync` als Race-Check vor dem Erzeugen der Pässe aufrufen (analog Event-Tickets). Wer `SeasonPrice.Categories` liest: neuer Element-Typ (`PassPrice` statt `SalePrice`, plus `TicketPrice`/`Offered`). `AdultReduced`/`YouthReduced` existieren im Code nicht mehr. **S6**: Kategorie-Labels heissen neu „Sonderaktion …" (via `DisplayName()` automatisch). Der `TicketPrice` je Kategorie ist als Standard-Ticketpreis der Saison gedacht (Anlässe können ihn künftig als Default ziehen — noch nicht verdrahtet). |
| 05.07.2026 | S2 | **Inline-Edit-Fundament in `main` (`51fc8b8`).** Neue Bausteine in `Features/Ticketing/Admin/`: `InlineTextEdit`, `InlineNumberEdit` (`decimal?`, `Decimals`/`Min`/`AllowEmpty`/`Prefix`), `InlineDateEdit` (`DateOnly?`), `InlineTimeEdit` (`TimeOnly?`), `InlineSelectEdit<TValue>` (`Options` + `Label`-Func, optional `Display`-RenderFragment für Badges) — alle erben `InlineEditBase<TValue>`. Verhalten: Zellwert klicken → Editor in der Zelle → Änderung öffnet `ConfirmDialog` („X von ‚alt' auf ‚neu' ändern?") → `Save`-Func (`Func<TValue, Task>`, NICHT EventCallback) läuft in try/catch, Fehler erscheinen im Dialog statt den Circuit zu töten. ESC bricht ab. CSS: `ta-inline`/`ta-inline-editor`/`ta-inline-num`. | **S4/S5/S6**: auf `main` rebasen und für Inline-Edits diese Komponenten nutzen, z. B. `<InlineSelectEdit TValue="TicketStatus" Value="@t.Status" Options="@Statuses" Label="@(s => s.DisplayName())" FieldLabel="Status" Save="@(s => SaveStatusAsync(t, s))" />`. Save-Delegat darf `DomainException` werfen (wird im Dialog angezeigt). |
| 05.07.2026 | S4 | **Öffentlicher Saisonkarten-Kauf (in main, `30f3a41`):** `CartItem` hat neu `CartItemKind Kind` (EventTicket/SeasonPass) + `int SeasonId`; `Cart`-Key enthält jetzt den Typ (`{kind}:{refId}:{cat}`). `ICartService` hat neu `AddSeasonPass(...)`. Neuer Endpoint `POST /warenkorb/add-saisonkarte`. `CheckoutController.Pay` verzweigt je Item-Typ: Saisonkarten-Items erzeugen `SeasonPass.Create(...)` + Token `TicketType.SeasonPass` + Mail; Kapazität via `ISeasonPassPricing.GetAvailableAsync`. Kauf-Buttons je Kategorie in `TicketingHome.cshtml` (Abschnitt `#saisonkarten`). Kein DB-Schema geändert. | **S3** (Verkauf/Warenkorb): `CartItem`/`ICartService` erweitert (additiv, bestehende `Add`/Event-Tickets unverändert) — beim Rebase additiv auflösen; die Warenkorb-View listet gemischte Item-Typen über denselben `Key`. **S5**: sobald das Sonderaktion-Preismodell über `ISeasonPassPricing` gemergt ist, dockt der Kaufflow automatisch an (nutzt denselben Port). |
| 05.07.2026 | S1 | **Freieinlass-Kategorien + SU-Kontingent (in main):** `IAdmissionService.GrantFreeEntryAsync`/`RevokeFreeEntryAsync` haben neu einen `FreeEntryType`-Parameter; Grant schreibt `TicketEventFreeEntries` (Kategorie), Revoke checkt nur Einlässe derselben Kategorie aus. Neue Tabelle **`TicketEventFreeEntryQuotas`** (`EventId` unique, `SuQuota` int null, via `EnsureTable`), Port `ISuFreeEntryQuota` (get/set), UI im Anlässe-Edit-Overlay; Scanner lehnt SU-Freieintritte über dem Kontingent ab. Neu `FreeEntryTypeExtensions.DisplayName()`. `FreeEntryListItem` hat neu `FreeEntryType? Category`. Tab „Freier Einlass" steht jetzt nach Flextickets (`Tabs`-Array!). Scan-View kompakt (eine Handyseite). | Wer `IAdmissionService` mockt/aufruft: neue Signaturen. **S6** (Anlässe-Tab): Edit-Overlay hat neu das Feld „SU-Freieintritt-Kontingent" — beim Inline-Umbau übernehmen. **S2/S6**: `Tabs`-Array wurde umsortiert, beim Rebase additiv auflösen. |
| 05.07.2026 | S1 | Runde-2-Plan verteilt (dieses Dokument). | Alle: Runde-2-Branch anlegen, Paket abarbeiten. S2s Inline-Edit-Fundament zuerst; S5s Preismodell vor S4s Saisonkarten-Kauf-Feinschliff. |
| 05.07.2026 | S6 | **Admin-Tab „Bestellungen" (Branch `feature/s6-admin-bestellungen`, auf `main` rebased):** neuer Tab-Key `orders` (`AdminOrdersComponent`) + Port `IOrderAdminReport`/`OrderListItem` + Reader `OrderAdminReportReader` (joint `Orders` mit `EventTickets`/`SeasonPasses`, ordnet Items der Saison via Event-Ids bzw. `SeasonId` zu). **Kein Schema geändert** (nutzt bestehende `Orders.Status`/`PaidAt`; alle Bestellungen werden gelistet, `Draft` = „Unbezahlt"). **Hinweis Bezahlstatus:** der Checkout ruft aktuell noch `order.MarkPaid()` sofort auf (`CheckoutController.Pay`), d.h. echte „Unbezahlt"-Zeilen entstehen erst, wenn die asynchrone Payrexx-Abwicklung die Bestellung als `Draft` anlegt und erst per Provider-Callback auf `Paid` setzt. | **S4 (Kasse/Payrexx)**: Beim Bau der asynchronen Zahlung die Bestellung als `Draft` speichern und erst nach Provider-Bestätigung `MarkPaid()` aufrufen; die Bestellungen-Übersicht zeigt Paid/Unbezahlt dann automatisch. Wer einen neuen Admin-Tab hinzufügt: `Tabs`-Array + `switch` in `TicketingAdminComponent.razor` sind die Konfliktstelle (additiv auflösen). |
| 05.07.2026 | S1 | **Schema: `TicketEventVisits.Uuid`** (additiv, NVARCHAR(36) NULL, Migrationsschritt `freeentry-uuid`). Freieinlässe erhalten beim Gewähren eine eigene UUID (`AdmissionService.GrantFreeEntryAsync`); Alt-Zeilen bleiben NULL (Anzeige „—"). Neuer Admin-Tab „Freier Einlass" (`AdminFreeEntriesComponent`, Tab-Key `freeentries`) + Port `IFreeEntryAdminReport`; Erstellt-Zelle via S2s `AdminFormat`, Erstellerin aus dem CheckIn-Scanlog. **In main gemerged.** | Wer `EventVisitRecord` schreibt/liest: neue nullable Property `Uuid`. Kein bestehender Contract geändert. |
| 05.07.2026 | S5 | **Öffentliche Struktur (in main):** (1) `/` redirectet auf `/ticketing/` (scan-Host weiter auf `/scanntickets`). (2) `ticketingRoot` mit `headerText`/`headerImage` + Template `TicketingHome` (= neue Startseite). (3) Doctype `saisonsPromo` → `/saisons/` Werbeseite. (4) Port `ISeasonPassPricing.GetAvailableAsync(seasonId)`. (5) Seeder ohne Beispieldaten. (6) Zurück-Buttons auf `/ticketing/`. | **Alle**: Frische Installationen ohne Demo-Daten. Kaufbutton `/saisons/` verlinkt auf `/ticketing/#saisonkarten`; echter Saisonkarten-Kaufflow ist Runde-2-Aufgabe von S4. |
| 05.07.2026 | S2 | **Tabellen-Fundament (in main):** `CreatedByName`/`CreatedByEmail`-Spalten, `AdminIdentity` als CascadingParameter, Bausteine `ConfirmDialog.razor`, `VisitsOverlay.razor` + `IVisitLogReader`, `AdminFormat` (TicketNo/Initials/CreatedShort/CreatedTooltip), zentrale CSS `ta-ticketno`/`ta-badge-btn`/`ta-visits-link`/`ta-subtable`. | Beim Erstellen von Tickets/Karten `createdByName: Identity.Name, createdByEmail: Identity.Email` durchreichen. |
| 05.07.2026 | S3 | **In main:** `IQrCodeRenderer.RenderPng` + `GET /ticket/{token}/qr.png`; Kategorie-Labels mit Alter; `Tickets__QrSecret` auf dev+prod gesetzt → alte Ticket-Links/QRs aus der Testphase sind ungültig, neu erzeugen. | Wer `IQrCodeRenderer` mockt: neue Methode. |
| 05.07.2026 | S1 | **Scanner-Contract (in main):** `Occupancy.FreeInside`, `IAdmissionService.ScanCodeAsync` (8-Zeichen-Kurzcode über alle vier Ticket-Tabellen), `FreeEntry.DisplayName()` = „Freier Einlass". | `Occupancy`-Konstruktion/Mocks: neuer Member mit Default. |
