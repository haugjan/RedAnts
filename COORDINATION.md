# Session-Koordination v2 (Worktrees + Branches) — Runde 5: Bestellpositionen, Wallet, Admin-UX

> Worktree-Modell wie gehabt (`C:\development\RedAnts-s<N>`, eigener Branch je Session).
> Runde 1–4 sind komplett in `main` (inkl. Payrexx-Zahlung). Diese Runde bündelt
> mehrere Feature- und UX-Pakete: ein echtes Bestellpositionen-Modell mit Artikel-GUID,
> Google-Wallet/PDF-Auslieferung, sowie diverse Admin- und Public-Verbesserungen.

## Arbeitsmodell

Runde-5-Branch `feature/s<N>-r5-<kurz>` vom aktuellen `main` anlegen; frei arbeiten; sofort
committen; Schema-/Contract-Änderungen im Änderungs-Log ankündigen; vor Merge auf `main`
rebasen, Release-Build grün, mergen, pushen; `main` bleibt immer grün. Keine Kommentare im
Code (Rationale in `ARCHITECTURE.md`), Enums als int, keine Secrets im Code, kein
Co-Authored-By. Admin-Bausteine wiederverwenden (`InlineSelectEdit`, `InlineNumberEdit`,
`InlineDateEdit`, `ConfirmDialog`, `OrderLogOverlay`/`VisitsOverlay`-Muster, `AdminFormat`,
`AdminIdentity`-CascadingParameter). `.cshtml`/`.razor` durch Starten der App verifizieren
(`dotnet run` mit `ASPNETCORE_ENVIRONMENT=Development`), nicht nur durch den Build.

## Reihenfolge / Abhängigkeiten

**S1 ist Fundament und sollte zuerst mergen** (führt `OrderItems`-Tabelle + Artikel-GUID +
Order-Erzeugung überall ein). **S4, S5, S6 sind unabhängig und starten sofort.** **S2 und S3**
bauen auf S1s Fulfillment-/Positionen-Pipeline auf: parallel starten, aber nach S1s Merge
rebasen und sich additiv einhängen.

## Fixierte Entscheidungen (gelten für alle)

1. **Artikel-GUID:** Jeder kaufbare Katalog-Artikel (Event-Kategorie, Saisonkarten-Kategorie,
   Zusatzoption, Mitglieder-Kategorie, Flex/SeasonSingle) bekommt eine **persistierte, stabile
   GUID**. `OrderItems` speichert `ArticleGuid` + Menge + Preis + Label + verknüpfte Ticket-Uuids.
2. **Neue Event-Summenspalte heisst „Erwartete Zutritte"** = verkaufte Spieltickets +
   Saisonkartenbesitzer + Mitglieder + bisherige Freieinlässe. **Rot** wenn > Einlasskontingent;
   zusätzlicher **Hinweis** wenn > Einlasskontingent + verbleibende Verkaufstickets.
3. **„Bisherige Einlässe"**: rot wenn > Einlasskontingent, orange wenn ≤ 10 übrig.
4. **Event-Spaltenreihenfolge** (Ziel, entspricht bereits dem aktuellen Code): Einlasskontingent,
   Verkaufskontingent, Bisherige Einlässe, Verkaufte Spieltickets, **Erwartete Zutritte**, dann
   Gruppe „Eingelöste Zutritte". S4 fügt nur die neue Spalte + Einfärbungen hinzu.
5. **PDF-Library:** QuestPDF (MIT, keine nativen Abhängigkeiten).

## Aufgabenpakete Runde 5

| Session | Branch | Paket | Status |
|---|---|---|---|
| S1 | `feature/s1-r5-order-items` | Artikel-GUID + `OrderItems`-Tabelle; Bestellung+Positionen bei jeder Ticket-Erzeugung (inkl. Admin); Positionen-Overlay in Bestellungen; Gratis-only überspringt Payrexx | erledigt, in main gemerged (`c98236f`; App-Boot gegen Dev-DB verifiziert = OrderItems-Tabelle + ArticleGuid-Spalten/NEWID-Backfill angelegt, Build grün). Ticket-`OrderId`-Backlink für Mitgliederkarten/Bundles nachgezogen (`93a8318`, Build+Deploy grün). Rest-Follow-up: Saisonkarten-Import-Order (offen, tief) |
| S2 | `feature/s2-r5-addon-texts` | Zusatzoptionen: Vorabinfo (Info-Icon-Tooltip) + Nachkauf-Info (Bestätigung UND Mail) | erledigt, in main gemerged (`4e305c4`; auf S1 rebased, additiv an `FulfillAsync`/Snapshot eingehängt; App-Boot gegen Dev-DB verifiziert = `/seasons/` 200 mit Zusatzoptionen, Migration `seasonaddons-info-texts`, Build grün, 93 Domain-Tests grün) |
| S3 | `feature/s3-r5-wallet-pdf` | Google Wallet + PDF; Download-Links auf Ticket-Seite und in der Bestätigungsmail | erledigt, in main gemerged (`8cf0796`; PDF `/ticket/{token}/pdf` gegen Dev-DB verifiziert = gültiges %PDF, Wallet config-gated & 404 ohne Credentials, unabhängig von S1) |
| S4 | `feature/s4-r5-event-columns-links` | Event-Tabelle: neue Summenspalte + Einfärbungen; Link-Kopier-Overlay (Events + Seasons) | erledigt, in main gemerged (`591c250`; App-Boot gegen Dev-DB verifiziert inkl. Migrationen, Admin-Route auth-geschützt) |
| S5 | `feature/s5-r5-admin-infra` | Inline-Edit auch per Mausklick verlassen; Admin-Position (Tab/Saison/Anlass/Bundle) via URL merken | erledigt, in main gemerged (`682f5f7`; App-Boot gegen Dev-DB verifiziert, Admin-Route auth-geschützt 302, Public 200) |
| S6 | `feature/s6-r5-public-format-fixes` | iFrame-Slim „Nächster Anlass"; restliche Datumsfelder auf dd.MM.yyyy; Venue-Bug „Ort nicht änderbar"; CSV-QR-Spalte prüfen/vereinheitlichen | erledigt, in main gemerged (`c639985`; App-Boot gegen Dev-DB verifiziert, `/heute/embed` liefert 200 mit `frame-ancestors *` und Absolut-CTA, Venue-Picker-Config von UDI auf Key-GUID migriert) |

---

### S1 — Bestellpositionen-Fundament (`feature/s1-r5-order-items`) — zuerst mergen

- **Artikel-GUID:** stabile, persistierte GUID pro Katalog-Artikel (Event-Kategorie-Preis,
  Saisonkarten-Kategorie, `SeasonAddOn`, Mitglieder-Kategorie, Flex/SeasonSingle). Additive
  nullable `ArticleGuid`-Spalte je Katalog-Tabelle, einmalig befüllt (idempotent beim Boot).
- **Neue Tabelle `OrderItems`** (`OrderId`, `ArticleGuid`, `Kind` int, `RefId`, `Category` int,
  `Quantity`, `UnitPrice`, `Label`; additiv via `EnsureTable`). Domäne `OrderItem`, Port
  `IOrderItems` (`SaveAsync`, `GetByOrderAsync`). Ersetzt/ergänzt das bisherige `OrderAddOns` +
  `FulfillmentPayload`-Provisorium für die Positions-Darstellung.
- **`CheckoutController.FulfillAsync`** schreibt für jede Ticket-/Pass-/Add-on-Position eine
  `OrderItems`-Zeile. **Gratis-Skip:** `if (payrexx.Enabled && saved.TotalGross > 0m)` … `else
  { await FulfillAsync(...); }` — reine Gratis-Bestellungen (nur Kinderkarten o.ä.) laufen nicht
  durch Payrexx.
- **Admin-Erzeugung legt eine Bestellung an:** `AdminTicketsComponent`,
  `AdminSeasonCardsComponent`, `AdminMemberCardsComponent`, `AdminFlexTicketsComponent` erzeugen
  künftig über einen gemeinsamen Weg eine `Order` (Zahlart z.B. „Manuell/Admin", Status Paid,
  Käufer aus dem Formular) + `OrderItems`, und verknüpfen das/die Ticket(s) via `OrderId`.
- **`AdminOrdersComponent`:** Button „Positionen anzeigen" → Overlay (Muster wie
  `OrderLogOverlay`), liest `IOrderItems.GetByOrderAsync`.
- Contract-Log: neue Tabelle/Spalten, `IOrderItems`, evtl. neuer `IOrderFactory`/Service für die
  Admin-Order-Erzeugung.

### S2 — Zusatzoptionen-Texte (`feature/s2-r5-addon-texts`) — nach S1

- `SeasonAddOn` + `SeasonAddOns`-Tabelle: zwei additive Textspalten `InfoBeforePurchase`
  (Vorabinfo) und `InfoAfterPurchase` (Nachkauf-Info). Admin-Add-on-Editor
  (`AdminSeasonsComponent`, Zusatzoptionen-Modal): zwei Textareas + Spalten.
- **Vorabinfo:** auf `SaisonsPromo` als **Info-Icon mit Tooltip** hinter der Option (nur wenn Text
  gesetzt).
- **Nachkauf-Info:** wird für die gewählten Optionen durch die Fulfillment-Pipeline
  (`FulfillmentSnapshot`/`FulfillAsync`) gereicht und in `Views/Checkout/Confirmation.cshtml`
  **und** in der Bestätigungsmail (`OrderMailer`) ausgegeben.
- Additiv zu S1: S2 erweitert die von S1 geformte Fulfillment-/Positionen-Pipeline; nach S1s
  Merge rebasen.

### S3 — Google Wallet + PDF + Auslieferung (`feature/s3-r5-wallet-pdf`) — nach S1

- Neuer `IWalletPass`-Service (Google Wallet: JWT-„Save to Google Wallet"-Link je Ticket) und
  PDF-Service (QuestPDF, Ticket mit QR). Config `GoogleWallet:IssuerId` /
  `GoogleWallet:ServiceAccountJson` (User liefert, siehe unten).
- **Ticket-Seite** (`WebTicketController` `/ticket/{token}`, `Views/WebTicket.cshtml`): Links
  „Als Google Wallet speichern" und „Als PDF herunterladen" (neue Endpunkte, z.B.
  `/ticket/{token}/wallet` und `/ticket/{token}/pdf`).
- **Bestätigungsmail** (`OrderMailer`): pro Ticket dieselben zwei Download-Links.
- Fallback: ohne Wallet-Credentials bleibt der Wallet-Link inaktiv/versteckt, PDF funktioniert
  unabhängig.

### S4 — Event/Saison-Admin-Tabellen (`feature/s4-r5-event-columns-links`) — sofort

- **`AdminEventsComponent`:** neue Spalte **„Erwartete Zutritte"** (nach „Verkaufte Spieltickets")
  mit Einfärbung (rot / Hinweis) sowie **„Bisherige Einlässe"** rot/orange (siehe fixierte
  Entscheidungen). `EventAdmissionReport`/`EventAdmissionCounts` um Saisonkarten-Bestand,
  Mitglieder-Bestand und Freieinlässe erweitern, damit die Summe berechnet werden kann.
- **Link-Overlay:** Klick auf die „Link"-Spalte (Events **und** Seasons) öffnet ein Overlay mit
  öffentlichem + internem Link, je einem „In Zwischenablage kopieren"-Button; Statushinweise: bei
  Status Intern Hinweis am öffentlichen Link, bei weder öffentlich noch intern Hinweis an beiden.
  Bestehendes `Copy(...)` + `EventLinks`/`SeasonLinks` wiederverwenden.

### S5 — Admin-Infrastruktur (`feature/s5-r5-admin-infra`) — sofort

- **Inline-Edit per Mausklick verlassen:** `InlineEditBase`-Ableitungen (`InlineTextEdit`,
  `InlineSelectEdit`, `InlineNumberEdit`, `InlineDateEdit`, `InlineTimeEdit`) sollen den
  Editiermodus nicht nur per ESC, sondern auch per Blur/Click-away schliessen (zentraler
  Mechanismus, analog dem bestehenden Blur in `DateBox`).
- **Admin-URL-State:** `TicketingAdminComponent` (heute nur `_tab` in-memory) + `TicketingAdminState`
  sollen den aktuell gewählten **Tab + Saison + Anlass/Bundle** als Query-Parameter in die URL
  schreiben (`NavigationManager`) und beim Laden wiederherstellen. Kopierter Link → gleiche
  Position.

### S6 — Public-Slim + Format + Fixes (`feature/s6-r5-public-format-fixes`) — sofort

- **iFrame-Slim „Nächster Anlass":** abgespeckte Ansicht via Query-Parameter (ohne Site-Nav/Footer)
  mit Titel, Heim-vs-Gegner-Logos und „Hier Tickets kaufen"-Link. Eigene Route/Layout für die
  iFrame-Einbindung auf der Hauptseite.
- **Datumsformate:** die noch nicht in `dd.MM.yyyy` dargestellten Felder finden (v.a. native
  Date-Inputs in Create-Modals) und auf das `DateBox`/`InlineDateEdit`-Muster („Verkauf bis")
  bringen. (Der Grossteil der Admin-Anzeigen ist bereits `dd.MM.yyyy`.)
- **Venue-Bug „Ort nicht änderbar":** `EnsureVenuePicker` in `TicketingContentTypeSeeder`
  (startNodeId / Datentyp-Konfiguration des eingeschränkten Venue-ContentPickers) prüfen und
  fixen, sodass der „Ort" am Event wieder wählbar ist.
- **CSV-QR-Spalte:** alle Exporte (`EventBundleExportController`, `FlexBundleExportController`,
  `SeasonPassExportController`, `MemberExportController`) enthalten bereits eine QR-taugliche
  Spalte (`Link`/`QrUrl` = voller Ticket-Token-URL). Prüfen/vereinheitlichen (Benennung) und bei
  neuen Exporten (falls S1 einen Positions-Export ergänzt) mitziehen.

## Vom Nutzer benötigt (blockierend für Live)

1. **Google Wallet (S3):** **Issuer-ID** + **Service-Account-JSON** (Google Cloud, „Google Wallet
   API" aktiviert), als Azure-App-Settings `GoogleWallet__IssuerId` / `GoogleWallet__ServiceAccountJson`.
   Ohne diese baut S3 die Integration, testet aber nicht end-to-end; PDF funktioniert unabhängig.

---

## Änderungs-Log (Schema/Contracts, die andere betreffen)

Neueste zuerst. Nur Änderungen eintragen, die andere Sessions betreffen.

| Datum | Session | Was | Auswirkung |
|---|---|---|---|
| 22.07.2026 | S3 | **Ort (Venue) auf dem Online-Einzelticket (in `main`, Ad-hoc-Auftrag).** Die Web-Ticket-Ansicht (`WebTicketController`/`WebTicket.cshtml`) löst bei Spieltickets neu den Event-Ort über `IVenues.FindByIdAsync(ev.VenueId)` auf und zeigt eine „Ort"-Zeile unter dem Datum. **`TicketCardModel` bekommt additiv ein optionales `VenueName`** (default null, gerendert nur wenn gesetzt); `WebTicketViewModel` ebenso. `WebTicketController` injiziert neu `IVenues`. Confirmation-/Processing-Karten und das PDF sind unverändert (zeigen den Ort noch nicht). | Wer `TicketCardModel`/`WebTicketViewModel` konstruiert: neues optionales Feld `VenueName` am Ende, bestehende Aufrufe unberührt. Kein Schema/Contract. Offen (falls gewünscht): Ort auch im PDF-Ticket und auf den Checkout-Bestätigungskarten. |
| 22.07.2026 | S5 | **Zahlungsabbruch führt zurück zum Checkout (in `main`, Ad-hoc-Auftrag).** Beim Abbrechen/Fehlschlagen der Payrexx-Zahlung leitete Payrexx nur das Overlay-iFrame auf CancelUrl/FailedUrl um, das Hauptfenster blieb auf der Bezahlseite hängen (Order bleibt `Draft`, das Status-Polling erkennt den Abbruch nie). Fix: `Views/Checkout/Cancelled.cshtml` bricht bei Framing aus dem iFrame aus (`window.top.location`) und leitet auf `/checkout?payment=aborted`; die `/checkout`-GET-Aktion zeigt dann „Die Zahlung wurde abgebrochen oder ist fehlgeschlagen…“. Warenkorb und Formulardaten bleiben erhalten. Gateway-`FailedUrl` zeigt jetzt auch auf `/checkout/cancel`. | Rein Checkout-UX, kein Schema/Contract. Wer `Views/Checkout/Cancelled.cshtml` oder die `/checkout`-GET-Aktion anfasst: neuer optionaler Query-Param `payment=aborted`. |
| 22.07.2026 | S3 | **Payrexx-Zahlung als In-Page-Overlay (in `main`, Ad-hoc-Auftrag).** Die Bezahlseite (`Views/Checkout/Payrexx.cshtml`) öffnete Payrexx bisher in einem separaten Fenster (erschien oft nicht). Jetzt bettet sie den Gateway-Link in ein selbstkontrolliertes iframe-Overlay ein; die Seite pollt `/checkout/status?order=` und navigiert bei `paid` das Hauptfenster auf `/checkout/success`, bei `cancelled` auf `/checkout/cancel`. Direkt-Link bleibt als Fallback. `CheckoutPayrexxView` bekommt additiv **`OrderId`** (fürs Polling gesetzt in `CheckoutController`). Das Payrexx-Modal-Skript entfällt. | Wer die Bezahlseite/`CheckoutPayrexxView` anfasst: neues Feld `OrderId`. Setzt CSP-`frame-src ... *.payrexx.com` voraus (bereits in `Program.cs`). Kein Schema/Contract. |
| 22.07.2026 | S3 | **Warenkorb-Session neustartfest (Azure SQL) (in `main`, Ad-hoc-Auftrag).** Die Session lag im Arbeitsspeicher, dadurch ging bei jedem App-Neustart (Deploy/Recycle/Scale-out) jeder Warenkorb verloren; ein Checkout nach einem Neustart sah einen leeren Korb und landete auf `/warenkorb` statt bei Payrexx. Neu: verteilter **`AddDistributedSqlServerCache`** auf der bestehenden `umbracoDbDSN`-DB, Tabelle **`AppSessionCache`** (idempotent beim Start via `SessionCacheSchema.Ensure`, in `Program.cs` vor `AddSession`). Neues NuGet **`Microsoft.Extensions.Caching.SqlServer`** (CPM `10.0.0`). DataProtection bleibt unangetastet (App Service persistiert die Schlüssel automatisch nach `%HOME%`). | Betrifft `Program.cs` (S6-Owner): additive Cache-Registrierung vor `AddSession`, beim Rebase mitziehen. Neue Tabelle `AppSessionCache` wird auf dev+prod beim Boot angelegt. Neues NuGet zentral gepinnt. Kein Ticketing-Contract. |
| 21.07.2026 | S5 | **Route `/heute` → `/next` umbenannt (in `main`, Ad-hoc-Auftrag).** Der öffentliche Nächster-Anlass-Redirect und das iFrame-Widget heissen jetzt **`/next`** und **`/next/embed`** (Controller `TodayController` → `NextController`); die alten `/heute`-Routen entfallen (404). `Program.cs` (Embed-CSP-Guard `isEmbed`) und `WarmupController` (Core-Pfade) mitgezogen. | **Wer das Widget einbettet: iFrame-`src` von `/heute/embed` auf `/next/embed` ändern.** Betrifft `Program.cs`/`WarmupController` (S6-Owner), rein Route-Umbenennung, kein Schema/Contract. |
| 21.07.2026 | S3 | **Transaktionsdaten geleert (Dev bereits ausgeführt, Prod via Deploy) (in `main`, Ad-hoc-Auftrag).** Neuer token-gesteuerter Einmal-Startup-Handler `TransactionalDataPurge`: leert bei gesetztem `Ticketing:PurgeTransactionalDataToken` einmalig (KeyValue-Guard je Tokenwert) die Tabellen `Orders`, `OrderItems`, `OrderAddOns`, `OrderStatusLogs`, `EventTickets`, `SeasonSingleTickets`, `SeasonPasses`, `MembershipCards`, `FlexTicketBundles`, `EventTicketBundles`, `TicketEventVisits`, `TicketEventVisitsLogs`, `TicketEventFreeEntries` und löscht alle `scanHelper`-Members. **Saisons, Anlässe, Spielorte sowie alle Preise/Stufen/Zusatzoptionen/Freieintritts-Kontingente bleiben erhalten.** Dev ist bereits geleert (Token `reset-2026-07-21`: 9 Orders, 12 EventTickets, 10 SeasonSingleTickets, 6 SeasonPasses, 6 MembershipCards, 1 FlexBundle, 0 Helfer). `deploy.yml` setzt denselben Token auf dev+prod App Service (dev = No-op durch Guard, prod purged einmalig beim Neustart). | **Achtung alle Sessions:** die geteilte Dev-DB hat keine Bestellungen/Tickets/Karten/Scans/Helfer mehr. Ohne gesetzten Token ist der Handler inert. Kein Schema/Contract geändert. |
| 21.07.2026 | S5 | **Helfer-Event-Einschränkung durchgesetzt (in `main`, Ad-hoc-Auftrag).** Ein nur für bestimmte Anlässe freigeschalteter Helfer konnte trotzdem fremde Events scannen: die Scan-Event-Liste war ungefiltert. Fix: die Helfer-Session in `Program.cs` (`/scan`) reicht neu `HelperAllEvents` (bool) + `HelperEventIds` (CSV) über `HttpContext.Items` an `ScanTickets.cshtml`; `TicketScanner` bekommt Parameter **`AllEvents`** + **`AllowedEventIds`** und filtert die wählbaren Events auf die Zuweisung (nur wenn Helfer-Login und nicht `AllEvents`). In Blazor Server ist die gerenderte Liste die Durchsetzung (der Client kann `_selected` nicht frei setzen). Rein additiv, kein Schema/Port. | Betrifft `Program.cs` (S6-Owner): kleiner additiver Block im gate-gekapselten Helfer-Middleware-Teil. `TicketScanner` hat zwei neue optionale Parameter. Kein DB-/Contract-Change. |
| 21.07.2026 | S5 | **Zahlart „Rechnung" ergänzt (in `main`, Ad-hoc-Auftrag).** `PaymentSource` bekommt additiv **`Invoice`** (int-Wert **7**, angehängt), DisplayName „Rechnung"; `AdminChoicesForPrice(price > 0)` bietet sie neben Cash/TWINT-Code/Terminal an. Rein additiv, kein Schema (die `Orders.PaymentSource`-Spalte speichert nur den int). | Wer `PaymentSource` als int mappt: Wert 7 = Invoice/Rechnung. Keine Signatur-/Schema-Änderung. |
| 21.07.2026 | S3 | **Rückerstattungs-Methode wählbar (in `main`, Ad-hoc-Auftrag).** Beim Rückerstatten einer bezahlten Bestellung fragt der Dialog jetzt nach der Methode: war die Bestellung online über Payrexx bezahlt (`PaymentSource.Online` + `TotalGross > 0`), gibt es die Wahl zwischen echter Payrexx-Rückerstattung und „auf anderem Weg erledigt (nur vermerken)"; sonst nur der manuelle Weg. **`IOrderStatusEditor.RefundAsync` hat neu einen `bool viaPayrexx`-Parameter** (Reihenfolge `orderId, viaPayrexx, changedBy`); bei `viaPayrexx` ohne Payrexx-Gateway wirft der Editor eine Klartext-`DomainException`. **`ConfirmDialog` bekommt optionalen `ChildContent`** (RenderFragment, unter der Nachricht), damit andere Sessions Auswahl-/Zusatz-UI im Dialog rendern können. `ticketing-admin.css?v=12` (neue `.ta-refund-choice`/`.ta-radio`/`.ta-modal-note`). | Wer `IOrderStatusEditor.RefundAsync` aufruft/mockt: neuer mittlerer Parameter `viaPayrexx`. `ConfirmDialog`-Nutzung unverändert kompatibel (ChildContent optional). Kein DB-Schema. Cache-Buster `ticketing-admin.css?v=12`. |
| 21.07.2026 | S2 | **Helfer als Umbraco-Members + Auffälligkeiten/Einladung (in `main`, Ad-hoc-Auftrag).** Die eigene `Helpers`-NPoco-Tabelle ist abgelöst durch einen code-first Membertyp **`scanHelper`** (Seeder in `HelperMemberTypeSeeder`; Properties `helperCode`/`helperFirstName`/`helperLastName`/`helperSeasonId`/`helperAllEvents`/`helperEventIds`). `IHelpers` ist neu member-gestützt (`HelperMemberRepository`, Code = Member-`Username`, deshalb `/scan`-Login unverändert); Domäne `Helper` hat neu `Code`, `AllEvents`, `EventIds`, Pflicht-`Email` (validiert) statt `Password`. **`IHelpers` erweitert** um `SetAssignmentAsync`; `AddAsync` erwartet neu eine gültige E-Mail. **`IEmailSender` hat neu eine Overload mit `IReadOnlyList<EmailAttachment>?`** (Brevo-Anhänge); neuer Port **`IHelperInviteMailer`**. Helfer-Admin: Auffälligkeits-Spalten (verschiedene Events Ein/Aus + Total-Scans), Event-Zuweisung, Löschschutz bei vorhandenen Scans, Einladungs-Mail mit Zugang + PDF-Anhang (`wwwroot/downloads/scanner-anleitung.pdf`, Pfad via `Scanner:GuidePdfPath`). | Wer `IHelpers`/`IEmailSender` implementiert oder mockt: neue Member (`SetAssignmentAsync`; `SendAsync`-Overload mit Attachments). Wer `Helper` konstruiert: `Password`→`Code`, E-Mail nun Pflicht, neue Felder `AllEvents`/`EventIds`. Neuer Membertyp `scanHelper` wird beim Boot angelegt. Die alte `Helpers`-Tabelle wird nicht mehr erzeugt/genutzt (Bestand verworfen, „neu beginnen"). Kein neues NPoco-Schema. |
| 21.07.2026 | S3 | **Bestell-Aktionen statt Status-Dropdown + Payrexx-Rückerstattung (in `main`, Ad-hoc-Auftrag).** Das freie Status-Dropdown in `AdminOrdersComponent` ist entfernt; stattdessen zustandsabhängige, per `ConfirmDialog` bestätigte Aktionen: Draft → „Als bezahlt" + „Stornieren"; Paid → „Stornieren" + „Rückerstatten"; Cancelled/Refunded = terminal (keine Aktion). **`IOrderStatusEditor` hat neu `RefundAsync(orderId, changedBy)`** (Payrexx-paid → echte Rückerstattung über Payrexx vor dem Statuswechsel; schlägt der Refund fehl, bleibt der Status; Nicht-Payrexx-Order = manueller Refund). **`IPayrexxGateway` hat neu `RefundGatewayAsync(gatewayId)`** (holt die bestätigte Transaction zum Gateway und ruft `POST /Transaction/{id}/refund`). `UmbracoOrderStatusEditor` injiziert neu `IPayrexxGateway`. `ticketing-admin.css?v=10` (neue `.ta-status-actions`/`.ta-link-danger`). | Wer `IOrderStatusEditor`/`IPayrexxGateway` implementiert oder mockt: je eine neue Methode. Wer `UmbracoOrderStatusEditor` konstruiert: neuer Ctor-Parameter `IPayrexxGateway`. Kein DB-Schema. Cache-Buster `ticketing-admin.css?v=10`. |
| 21.07.2026 | S1 | **SEO + Dev-No-Index + Admin-Tabellenbreite + Restmengen + Fix-Freieintritte (in `main`, mehrere Commits bis `6361b2b`).** SEO-Meta/OG/Twitter/JSON-LD im `_SiteLayout`, neuer `SeoController` (`/robots.txt`, `/sitemap.xml`); **host-basiertes No-Index** (nur `*.redants.ch` = `index,follow`, alles andere `noindex`). Admin-Preistabelle kompakter (CSS `v=9`). Restmengen als Badge auf `TicketEvent`/`SaisonsPromo`. **Freieintritte vorab fix erfassbar:** additive Spalten `SuFixed`/`PlayerFixed`/`StaffFixed`/`OfficialFixed`/`ChildFixed` auf `TicketEventFreeEntryQuotas` (Migration `freeentry-fixed-counts`); **`IFreeEntryQuota` hat neu `GetFixedAsync` und `SetAllAsync(eventId, quotas, fixedCounts)`** (zweiter Pflicht-Parameter); Fix-Anzahl verbraucht das Kontingent und zählt in Belegung + erwartete/bisherige Zutritte. | Wer `IFreeEntryQuota.SetAllAsync` aufruft: neuer zweiter Parameter `fixedCounts`. Nur additive DB-Spalten. Public-Views (`_SiteLayout`, `TicketEvent`, `SaisonsPromo`) additiv erweitert (koexistiert mit S6s 900px/CSP). Hinweis an S6: das JSON-LD ist ein `application/ld+json`-Block (kein ausführbares JS). |
| 21.07.2026 | S6 | **Public-Auslieferung & Härtung (in `main`, `6377201`).** Alleiniger Owner von `Program.cs`, `appsettings*.json`, Public-Views/CSS, PDF-/Mail-Templates und dem Warmup-Teil von `deploy.yml`. Neu: globale **Security-Header-Middleware** (`X-Content-Type-Options`, `Referrer-Policy`, `Strict-Transport-Security` über HTTPS, `X-Frame-Options: SAMEORIGIN` ausser `/heute/embed`, konservative **CSP** nur für Public, Backoffice/Blazor/Scanner/Embed ausgenommen) plus Kestrel `AddServerHeader=false`. **Eigene 404-Seite** über eine Response-Buffering-Middleware + statische `wwwroot/404.html` (Umbraco schreibt sonst seine eigene nonodes-Seite). **`RuntimeMode=Production`** in der Basis-Config, in `appsettings.Development.json` auf `BackofficeDevelopment` überschrieben; dazu `Global:UseHttps=true` (Prod-Validator) und `Global:Smtp:From=tickets@redants.ch`. Neuer **`/warmup`**-Endpoint (lädt Kernseiten serverseitig vor) + `deploy.yml` ruft `/warmup` und setzt Imaging-HMAC aus GitHub-Secret `IMAGING_HMAC_KEY`. Public-Content-Spalten auf einheitlich **900px**; Red-Ants-Logo im PDF-Ticket; E-Mail-Logo verkleinert. | Für andere Sessions **nicht-brechend**. Wer Public-Views hinzufügt: die CSP erlaubt nur `'self'` + Fonts/jsdelivr/Cloudflare-Turnstile/Google-Maps/Payrexx, sonst Quelle ergänzen. Backoffice/Blazor sind CSP-befreit. Content-Container-Breite = 900px. Neue Live-Secrets nötig: GitHub-Secret `IMAGING_HMAC_KEY` (sonst wird Imaging-HMAC übersprungen). |
| 21.07.2026 | S1 | **Mitgliederkarten von Bestellungen entkoppelt (in `main`, `08f1102`).** Korrigiert den vorigen S1-Eintrag: Mitgliederkarten sind Teil der Mitgliedschaft, kein Verkauf, und erzeugen/tragen **keine** Order mehr. `IMemberCards.CreateAsync` **verliert den `orderId`-Parameter**; die Admin-Erfassung fragt keine Zahlart mehr ab und legt keine Order an. Der Bestell-Report (`OrderAdminReportReader`) und `OrderListItem` führen **keine Mitgliederkarten** mehr (Felder `MemberCardCount`/`MemberCardSummary` entfernt; Flextickets + Zahlart bleiben). Startup-Cleanup `MemberCardOrderCleanup` setzt bestehende `MembershipCards.OrderId` auf NULL und löscht Mitgliederkarten-`OrderItems` (idempotent). | Wer `IMemberCards.CreateAsync` aufruft: `orderId`-Parameter existiert nicht mehr. Wer `OrderListItem` konstruiert/mockt: zwei Felder weniger (`MemberCardCount`/`MemberCardSummary`). Phantom-Alt-Orders (nur Mitgliederkarte) bleiben unsichtbar bestehen (Report überspringt Orders ohne Verkaufsposition). |
| 21.07.2026 | S2 | **Add-on Titel + Stufen-Beschränkung (in `main`, `e7f1e05`).** `SeasonAddOns` bekommt zwei additive Spalten **`LongTitle`** (nvarchar 500, öffentlicher Bewerbungstitel) + **`AllowedTierIds`** (nvarchar 500, CSV der erlaubten Basis-Stufen; leer/NULL = alle Stufen); Migration `seasonaddons-title-tiers`. `SeasonAddOn` trägt neu `LongTitle` (string?) + `AllowedTierIds` (`IReadOnlyList<int>`); `Create`/`FromPersistence` haben zwei neue optionale End-Parameter `longTitle`, `allowedTierIds`. Admin-Zusatzoptionen-Modal (`AdminSeasonsComponent`) hat neu eine Langtitel-Spalte + eine Stufen-Mehrfachauswahl (nur Nicht-Promo-Stufen). `SaisonsPromo` bewirbt den Langtitel (Fallback Kurztitel) und bietet eine Option nur für ihre erlaubten Stufen an; die Beschränkung matcht die **Basis-Stufe** (greift also auch, wenn eine Promo-Variante angeboten wird). `CartController.AddSeasonPass` filtert gewählte Add-ons serverseitig gegen dieselbe Stufen-Regel. | Wer `SeasonAddOn.Create`/`FromPersistence` beim Aktualisieren aufruft: zwei neue optionale End-Parameter (Default null/leer). Nur additive Spalten (`LongTitle`, `AllowedTierIds`). `CartController` injiziert neu `IPriceTiers`. Kein Umbau bestehender Call-Sites nötig. |
| 21.07.2026 | S1 | **Zahlarten-Fundament (in `main`, `abcd79c`).** Neues Enum **`PaymentSource`** (Sponsoring, Marketing, Goodwill, Online, Cash, TwintCode, Terminal) + `PaymentSourceExtensions.DisplayName`/`AdminChoicesForPrice(price)`. `Order` trägt neu **`PaymentSource?`** (additive nullable int-Spalte **`Orders.PaymentSource`**, Migration `order-payment-source`); `Order.Create` und `Order.FromPersistence` haben je einen neuen optionalen End-Parameter `paymentSource`. **`IAdminOrderFactory.CreateAsync` hat neu einen Pflicht-Parameter `PaymentSource paymentSource`** (am Ende); alle 5 Admin-Create-Wege (Einzelticket, Einzel-Saisonkarte, Mitgliederkarte, Flex-Bundle, Event-Bundle) liefern ihn aus einem preisgefilterten Selektor; Online-Checkout setzt `PaymentSource.Online` automatisch. **`OrderListItem`** hat vier neue Felder (`MemberCardCount/Summary`, `FlexTicketCount/Summary`, `PaymentSource?`); die Bestellübersicht zeigt Zahlart + Mitgliederkarten + Flextickets und blendet Member-Card-/Flex-only-Orders nicht mehr aus. | Wer `IAdminOrderFactory.CreateAsync` aufruft: neuer Pflicht-Parameter `paymentSource` am Ende. Wer `Order.Create`/`FromPersistence` aufruft: neue optionale End-Parameter (Default null). Wer `OrderListItem` konstruiert/mockt: vier neue Felder. Additive Spalte `Orders.PaymentSource`. Preisregel-Helper: `PaymentSourceExtensions.AdminChoicesForPrice`. |
| 21.07.2026 | S3 | **Preis/Stufen-UX (in `main`, `ce70ef1`).** Neue **editierbar-vs-fix-Konvention**: `.ta-inline` (die Klick-Fläche von `InlineTextEdit`/`InlineSelectEdit`/`InlineNumberEdit`/`InlineDateEdit`/`InlineTimeEdit`) trägt jetzt dauerhaft eine gestrichelte Unterlinie (Hover = Akzent); feste Werte = Klartext ohne Unterlinie. `ticketing-admin.css?v=7`. Season-Preis-Modal: Sonderaktion je Stufe per `.ta-promo-toggle`-Pill klar an/aus (auch für eigene Stufen); `SaveEdit` nummeriert `SortOrder` neu für eindeutige Paarung. Event-Tabelle: „Verkaufte Spieltickets"/„Bisherige Einlässe" getauscht + Header-Tooltip. | **Konvention für alle Admin-Sessions:** editierbare Zellen über die `Inline*Edit`-Komponenten (`.ta-inline`) rendern, feste Werte als Klartext — so sind sie automatisch unterscheidbar. Wer `ticketing-admin.css` versioniert: neuer Cache-Buster `v=7`. Kein Schema/Port. |
| 21.07.2026 | S4 | **Scanning-UX + Zugang (in `main`, `ac5ac25`).** Scanner-Eventauswahl (`TicketScanner.razor`) hebt Anlässe von **heute** hervor (grünes „Heute"-Badge + Rahmen); Auswahl eines nicht-heutigen Anlasses zeigt eine scanner-eigene Rückfrage („Dieser Anlass ist nicht heute. Bist du sicher?") und fährt bei Bestätigung fort. (Statt `ConfirmDialog`, weil die Scanner-Seite nur `ticket-scanner.css` lädt; Bestätigungsstyles liegen im `<style>`-Block der Komponente.) **`Program.cs`:** der Site-Gate akzeptiert das Zugangspasswort (`BasicAuth:Password`) jetzt zusätzlich per Query-Parameter **`?key=`** (setzt das Gate-Cookie und leitet auf die bereinigte URL um); Formular unverändert. | Kein Schema/Port. **Achtung S6** (Owner `Program.cs`): additive Gate-Erweiterung im `BasicAuth`-Block (neuer `SetGateCookie`-Helfer + `?key=`-Zweig); beim Rebase mitziehen. |
| 21.07.2026 | S5 | **Helfer-Rework (in `main`, `70f78f9`).** `AdminHelpersComponent` erfasst Helfer jetzt per Overlay statt inline; die Sektion „Scans pro Anlass" ist entfernt. Neue Icon-Spalte je Helfer öffnet ein Overlay mit dessen Scan-Übersicht pro Anlass (Einlässe/Auslässe separat). Rein UI-seitig: keine Port-/Schema-Änderung, `IHelperScanReport.GetByEventsAsync` unverändert weiterverwendet; Helfer↔Scan-Zuordnung über `ScannedBy == Helper.FullName` (Rationale in `ARCHITECTURE.md`). | Keine Auswirkung auf andere Sessions (kein Contract/Schema geändert). |
| 21.07.2026 | S2 | **Zusatzoptionen-Texte (in `main`, `4e305c4`).** `SeasonAddOns` bekommt zwei additive Textspalten **`InfoBeforePurchase`** (Vorabinfo) + **`InfoAfterPurchase`** (Nachkauf-Info); Migration `seasonaddons-info-texts`; `SeasonAddOn.Create/FromPersistence` haben zwei neue optionale End-Parameter. Admin-Zusatzoptionen-Modal (`AdminSeasonsComponent`) hat zwei neue Textarea-Spalten; `SaisonsPromo` zeigt die Vorabinfo als Info-Icon-Tooltip. **`FulfillmentAddOn` hat neu ein erstes Feld `int Id`** (Snapshot; alte Drafts ohne `Id` deserialisieren tolerant zu `Id=0`). `CheckoutController.FulfillAsync` gibt neu `(Tickets, AddOnInfos)` zurück; `CheckoutConfirmationView` hat neu `AddOnInfoTexts`. **`OrderMailModel` hat neu optionalen letzten Parameter `AddOnInfoTexts`**; `OrderMailer` rendert die Nachkauf-Info additiv (koexistiert mit S3s `IWalletPass`), `Confirmation.cshtml` zeigt sie ebenfalls. | Wer `OrderMailModel` konstruiert: neuer optionaler letzter Parameter (Default null). Wer `FulfillmentAddOn` konstruiert: neues erstes Feld `Id`. Wer `SeasonAddOn.Create/FromPersistence` beim Aktualisieren aufruft: neue optionale Info-Parameter. Nur additive Spalten. |
| 21.07.2026 | S1 | **OrderItems-Fundament (in `main`, `c98236f`).** Neue Tabelle **`OrderItems`** (`OrderId`, `Kind` int, `ArticleGuid` guid?, `RefId`, `Category` int, `Label`, `Quantity`, `UnitPrice`) + Domäne `OrderItem` + Enum `OrderItemKind` (EventTicket/SeasonSingle/SeasonPass/MemberCard/AddOn) + Port **`IOrderItems`** (`SaveAsync(orderId, items)`, `GetByOrderAsync`). Additive **`ArticleGuid`** (nullable) auf `EventPriceCategories`/`SeasonPriceCategories`/`SeasonAddOns` mit idempotentem `NEWID()`-Backfill. Neuer Enum-Wert **`PaymentMethod.Manual`** (int 4, angehängt). Neuer Port **`IAdminOrderFactory`** (`CreateAsync(buyer, email, lines, createdBy)` → legt Order (Manual/Paid) + OrderItems + Order-Log an); alle 4 Admin-Create-Wege (Einzelticket, Einzel-Saisonkarte, Mitgliederkarte, Flex-/Event-Bundle) legen jetzt darüber eine Bestellung an. **`FulfillAsync` schreibt OrderItems**; **Gratis-Skip**: Payrexx nur bei `payrexx.Enabled && saved.TotalGross > 0m`. Positionen-Overlay `OrderItemsOverlay` in `AdminOrdersComponent`. | S2 baut additiv auf `FulfillAsync`/`FulfillmentSnapshot` auf (jetzt rebasen). Wer `PaymentMethod` als int mappt: Wert 4 = Manual. Wer die 3 Katalog-Records baut: neue nullable `ArticleGuid`-Spalte. **Follow-up offen:** Ticket-`OrderId`-Backlink für Mitgliederkarten/Bundles + Saisonkarten-Import-Order (Positionen sind erfasst, aber die erzeugten Karten/Bundle-Tickets tragen die `OrderId` noch nicht). |
| 21.07.2026 | S1 | **OrderId-Backlink nachgezogen (in `main`, `93a8318`).** `EventTicket.CreateForBundle`/`SeasonSingleTicket.CreateForBundle` + Ports `IMemberCards.CreateAsync`/`IFlexTicketBundles.CreateAsync`/`IEventTicketBundles.CreateAsync` erhalten optionalen `int? orderId = null`; die Repos setzen ihn auf die Ticket-Zeile (`OrderId`). Die 3 Admin-Wege (Mitgliederkarte, Flex-Bundle, Ticket-Bundle) fangen die erzeugte Order und reichen `order.Id` durch. Damit tragen ALLE backoffice-erzeugten Tickets/Karten die `OrderId`. | Rein additiv (neue optionale Parameter am Ende der Signaturen), kein Umbau bestehender Call-Sites nötig. Rest-Follow-up: Saisonkarten-CSV-Import legt noch keine Order an (`MemberCardRepository.ImportAsync`, offen). |
| 21.07.2026 | S6 | **Public-Slim + Format + Fixes (in `main`, `c639985`).** Neue öffentliche Route **`/heute/embed`** rendert ein schlankes iFrame-Widget (`Views/NextEventEmbed.cshtml`, `Layout = null`, `NextEventEmbedModel` in `Features/Ticketing/Public`) für den nächsten öffentlichen Anlass, sendet `Content-Security-Policy: frame-ancestors *` und verlinkt per `target="_top"` auf die absolute Kauf-URL. **CSV-QR-Spalte vereinheitlicht:** `MemberExportController` heisst die Ticket-URL-Spalte jetzt `Link` (vorher `QrUrl`) und schreibt `\r\n`, damit alle vier Exporte (`Event`/`Flex`/`SeasonPass`/`Member`) dieselbe QR-taugliche `Link`-Spalte tragen. Venue-Picker-Fix in `TicketingContentTypeSeeder.EnsureVenuePicker` (schreibt die Ordner-Key-GUID statt der UDI in `startNodeId`). Mitglieder-Geburtsdatum nutzt jetzt `<DateBox>` (dd.MM.yyyy). | Falls jemand einen weiteren QR-/Ticket-Export ergänzt: Spalte `Link` nennen (nicht `QrUrl`). Neue Route `/heute/embed` und View `NextEventEmbed.cshtml` sind reserviert. Venue-Fix ist reine DB-Config, kein Code-Contract. |
| 21.07.2026 | S3 | **Wallet/PDF-Auslieferung (in `main`, `8cf0796`).** Neue Ports `ITicketPdf` (QuestPDF) + `IWalletPass` (Google-Wallet-Save-JWT, config-gated `GoogleWallet:IssuerId`/`ServiceAccountJson`), registriert in `TicketDeliveryComposer` (setzt QuestPDF-Community-Lizenz). Neue Endpunkte `/ticket/{token}/pdf` und `/ticket/{token}/wallet`; `WebTicketViewModel` hat neu `Token` + `WalletEnabled`. **`OrderMailer` injiziert neu `IWalletPass`** und zeigt pro Ticket PDF-/Wallet-Links. Neue NuGet `QuestPDF`. | Wer `OrderMailer` konstruiert/mockt: neuer Ctor-Parameter `IWalletPass`. Wer `WebTicketViewModel` baut: zwei neue optionale Felder. S2 (Add-on-Nachkauftext in `OrderMailer`) additiv einhängen. Kein DB-Schema. |
| 21.07.2026 | S4 | **Event/Saison-Admin-Tabellen (in `main`, `591c250`).** `EventAdmissionCounts` hat zwei neue optionale Felder `SeasonPassHolders`/`MemberHolders` + berechnete Property `ExpectedAdmissions` (= verkaufte Spieltickets + Saisonkartenbesitzer + Mitglieder + bisherige Freieinlässe); `EventAdmissionReportReader` injiziert neu `IEvents` (Anlass→Saison-Zuordnung für die Bestandszahlen). Neue wiederverwendbare Komponente **`LinkOverlay.razor`** (öffentlicher + interner Link mit Kopier-Button + statusabhängige Hinweise). `AdminEventsComponent`: neue Spalte „Erwartete Zutritte" (rot bei Überschreitung Einlasskontingent, oranger Hinweis bei potenzieller Überbuchung), „Bisherige Einlässe" rot/orange; Link-Spalte (Events **und** Seasons) öffnet jetzt das `LinkOverlay` statt Direktkopie. | Wer `EventAdmissionCounts` konstruiert: zwei neue optionale Felder (Default 0). Wer `IEventAdmissionReport` implementiert/mockt bzw. `EventAdmissionReportReader` ersetzt: Reader braucht `IEvents`. Neuer Admin-Baustein `LinkOverlay` steht anderen Sessions zur Verfügung. Kein DB-Schema. |
| 21.07.2026 | S5 | `TicketingAdminState` ist jetzt beobachtbar (`event Changed`) und trägt zusätzlich `SelectedEventId` + `SelectedBundleId`. `SelectedSeasonId` etc. sind Properties, die `Changed` auslösen. `TicketingAdminComponent` spiegelt Tab/Saison/Anlass/Bundle in die URL-Query. | Wer `TicketingAdminState` neu nutzt: Property-Semantik (löst `Changed` aus), rein additiv. `InlineEditBase` schliesst Editiermodus zusätzlich per Blur (`HandleBlur`) und hat neu `EditorElement` (Auto-Fokus). Keine Signaturänderung an den Inline-Editoren. |
| 21.07.2026 | S1 | Runde-5-Plan verteilt (dieses Dokument). | Alle: Runde-5-Branch anlegen; S1 zuerst mergen, S2/S3 danach rebasen. |
| 21.07.2026 | S1 | **Payrexx-Prod-Fix (in `main`, `4da90bb`).** `deploy-prod` konfiguriert jetzt auch `Payrexx__Instance`/`Payrexx__ApiSecret` (vorher nur `deploy-dev`), sonst blieb Payrexx auf Prod deaktiviert und bezahlte Checkouts übersprangen den Gateway. | Keine Code-Auswirkung. Wenn `PAYREXX_API_SECRET` nur ein dev-Environment-Secret ist, muss es dem `prod`-Environment (oder Repo) hinzugefügt werden, sonst greift der Skip-Guard. |
| 21.07.2026 | S1 | **Runde-6-Plan verteilt** (Abschnitt „Runde 6" unten). | Alle: Runde-6-Branch anlegen; S1 zuerst mergen; S2 wartet automatisch auf S1 (Snippet im Plan); Rest startet sofort und rebast vor Merge. |
| 22.07.2026 | S4 | **Google Wallet komplett entfernt** (`feature/s4-remove-google-wallet`). Gelöscht: `IWalletPass`/`WalletTicketModel`, `GoogleWalletPass`, `/ticket/{token}/wallet`-Endpoint, Wallet-Button auf der Ticket-Seite, Wallet-Link in der Bestätigungsmail, `GoogleWallet`-Config in `appsettings.json`, die beiden „Configure Google Wallet"-Steps in `deploy.yml`. PDF-Auslieferung bleibt unverändert. | `IWalletPass` existiert nicht mehr (war nur per DI aufgelöst). `OrderMailer`- und `WebTicketController`-Ctor haben den `IWalletPass`-Parameter verloren; `WebTicketViewModel` das Feld `WalletEnabled`. Wer eines davon konstruiert/mockt: Parameter/Feld entfernen. |

---

# Runde 6 — Zahlarten, Add-on-Feinschliff, Admin-UX, Scanning, Helfer, Härtung

> Arbeitsmodell wie Runde 5 (eigener Worktree `C:\development\RedAnts-s<N>`, Branch
> `feature/s<N>-r6-<kurz>` vom aktuellen `main`; sofort committen; Schema-/Contract-Änderungen
> im Änderungs-Log ankündigen; vor Merge rebasen, Release-Build grün, `main` immer grün; keine
> Kommentare im Code, Enums als int, keine Secrets im Code, kein Co-Authored-By; `.razor`/`.cshtml`
> durch Starten der App verifizieren). Admin-Bausteine wiederverwenden (`InlineSelectEdit`,
> `InlineNumberEdit`, `InlineDateEdit`, `ConfirmDialog`, `LinkOverlay`, `OrderItemsOverlay`,
> `AdminFormat`, `AdminIdentity`).

## Automatische Koordination (Sessions warten selbst)

Nur **eine harte Abhängigkeit**: **S1 ist Fundament (Zahlarten) und merged zuerst.** **S2** wartet
automatisch auf S1, bevor es die Checkout-/Order-Teile anfasst. **S3, S4, S5, S6 starten sofort**
und rebasen nur vor dem Merge. Wait-Snippet (Bash), das die abhängige Session selbst ausführt und
erst weiterläuft, wenn S1s Fundament in `main` ist:

```bash
until git fetch -q origin && git cat-file -p origin/main:Domain/Ticketing/Sales/SalesEnums.cs 2>/dev/null | grep -q "enum PaymentSource"; do
  echo "warte auf S1-Fundament (PaymentSource)…"; sleep 30
done
git -c core.safecrlf=false rebase origin/main
```

## Aufgabenpakete Runde 6

| Session | Branch | Paket | Abhängig |
|---|---|---|---|
| S1 | `feature/s1-r6-payment-sources` | Zahlart (PaymentSource) bei JEDER Order-/Ticket-/Bundle-Erstellung, preisabhängig gefiltert; Fix „Mitgliederkarten erzeugen keine Bestellung" | erledigt, in main gemerged (`abcd79c`; App-Boot gegen Dev-DB verifiziert = Migration `order-payment-source` angewandt, `/seasons/` 200, keine Laufzeitfehler, Build grün, 93 Domain-Tests grün). S2 kann starten. |
| S2 | `feature/s2-r6-addon-titles-tiers` | Zusatzoptionen: kurzer + langer Titel; Zusatzoption optional nur für bestimmte Stufen (Tiers) | erledigt, in main gemerged (`e7f1e05`; auf S1 rebased, Migrations-Konflikt additiv gelöst; Migration `seasonaddons-title-tiers` = neue Spalten `LongTitle` + `AllowedTierIds`; App-Boot gegen Dev-DB verifiziert = `/seasons/` 200 mit Zusatzoptionen, Release-Build grün, 93 Domain-Tests grün) |
| S3 | `feature/s3-r6-pricing-ux` | Stufen/Preis-Verwaltung intuitiver (Sonderaktion aktiv/inaktiv klar; Sonderaktion auch für zusätzliche Stufen); editierbare vs. fixe Werte optisch unterscheidbar; Event-Tabelle: Spaltentausch + Tooltip | erledigt, in main gemerged (`ce70ef1`; Release-Build grün, App-Boot gegen Dev-DB verifiziert = Public 200, `/umbraco` 200, `ticketing-admin.css?v=7` mit neuen Klassen ausgeliefert; Admin-Modal build-kompiliert + logisch geprüft, Backoffice-Login zum Klicktest nicht verfügbar) |
| S4 | `feature/s4-r6-scanning` | Scanning-Eventauswahl: heutige Events hervorheben, Rückfrage (aber zulassen) bei Fremd-Tag; Zugangspasswort `stockschlag` auch per Query-Parameter | erledigt, in main gemerged (`ac5ac25`; Gate-`?key=`-Flow gegen Dev-DB verifiziert, Scanner-Route auth-geschützt) |
| S5 | `feature/s5-r6-helpers` | Helfer: Erfassung per Overlay; „Scans pro Anlass" entfernen; Icon-Spalte mit Helfer-Scan-Übersicht pro Anlass (Ein-/Ausgänge separat) | **erledigt, in main gemerged (`70f78f9`; App-Boot gegen Dev-DB verifiziert, Admin-Route auth-geschützt 302, Public 200, Release-Build grün)** |
| S6 | `feature/s6-r6-frontend-hardening` | Public-Breiten angleichen; eigene 404-Seite; PDF-Ticket-Logo; E-Mail-Logo-Grösse; Security-Header; RuntimeMode=Production; Notification-From-E-Mail; Imaging-HMAC; Warmup erweitern | erledigt, in main gemerged (`6377201`; App-Boot gegen Dev-DB verifiziert, Public 200 mit CSP, 404 liefert branded Seite mit 404-Status, `/heute/embed` behält `frame-ancestors *`, `/umbraco` CSP-befreit, `/warmup` warmt 7 Kernseiten, `Server`-Header entfernt, PDF-Logo via isoliertem QuestPDF-Render verifiziert, Release-Build grün; Prod-RuntimeMode über die vier Umbraco-Validatoren abgesichert) |

---

### S1 — Zahlarten-Fundament (`feature/s1-r6-payment-sources`) — zuerst mergen

- Neues Enum **`PaymentSource`** (int, `Domain/Ticketing/Sales/SalesEnums.cs`): `Sponsoring, Marketing,
  Goodwill, Online, Cash, TwintCode, Terminal`. Order bekommt eine additive int-Spalte **`PaymentSource`**
  (Migration additiv; `PaymentMethod` bleibt für die technische Zahlungsart Payrexx/Manual bestehen).
- **Regel (immer angeben):** Preis == 0 → nur `Sponsoring/Marketing/Goodwill`; Preis > 0 →
  `Cash/TwintCode/Terminal`; **`Online`** wird ausschliesslich vom Online-Checkout automatisch gesetzt
  (im Admin nicht wählbar).
- **Alle Admin-Erstellungswege** (Einzelticket, Einzel-Saisonkarte, Mitgliederkarte, Flex-Bundle,
  Event-Bundle) bekommen einen **Zahlart-Selektor** (nach Preisregel gefiltert) und geben die Zahlart an
  `IAdminOrderFactory.CreateAsync` (neuer Parameter). Zahlart in der Bestellübersicht anzeigen.
- **Fix „Mitgliederkarten erzeugen keine Bestellung":** Ursache prüfen (Admin-Weg ruft zwar
  `AdminOrderFactory`, aber Bestellung erscheint nicht/entsteht nicht) und sicherstellen, dass jede
  Mitgliederkarten-Erstellung eine sichtbare Order + Positionen erzeugt.
- P0 Payrexx-Prod-Config ist bereits erledigt (`4da90bb`); nur verifizieren, dass ein bezahlter Kauf auf
  Prod den Gateway öffnet.
- **Ankündigen:** `PaymentSource`-Enum, `Orders.PaymentSource`-Spalte, `AdminOrderFactory`-Signatur.

### S2 — Zusatzoptionen: zwei Titel + Tier-Beschränkung (`feature/s2-r6-addon-titles-tiers`)

- Wait-Snippet oben ausführen (wartet auf S1), dann arbeiten.
- `SeasonAddOn`: zwei additive Textfelder — **`ShortTitle`** (kurz, kommt auf Saisonkarte/Beleg/Mail,
  z. B. „Erwachsen - reduziert") und **`LongTitle`** (lang, Bewerbung auf der Seite, z. B. „Erwachsen -
  Erste 85 zum halben Preis inkl. Cüpli am ersten Spiel"). Bestehendes `Label` als Kurztitel weiterführen.
- **Tier-Beschränkung:** Zusatzoption optional nur für bestimmte Stufen gültig/angeboten (additive
  Verknüpfung AddOn↔Tier, z. B. CSV-Spalte `AllowedTierIds` oder Link-Tabelle). Leer = alle Stufen.
- Admin-Zusatzoptionen-Modal: beide Titel + Tier-Mehrfachauswahl. Public-Saison-Seite bewirbt den
  Langtitel; Kauf/Beleg/Mail nutzen den Kurztitel. Tier-Read über bestehende Tier-API (nicht ändern).

### S3 — Preis/Stufen-Verwaltung-UX + Tabellen (`feature/s3-r6-pricing-ux`)

- Stufen/Preis-Verwaltung intuitiver: **Sonderaktion je Stufe klar aktivierbar/deaktivierbar** (Toggle,
  klare Trennung Normalpreis vs. Sonderaktion) und **Bug beheben: für zusätzliche/eigene Stufen lassen
  sich keine Sonderaktionen mehr erfassen** → Sonderaktion für JEDE Stufe erfassbar machen.
- **Editierbare vs. fixe Werte** in Admin-Tabellen optisch unterscheidbar: eine wiederverwendbare
  CSS-Konvention (z. B. gestrichelte Unterlinie + Stift-Cursor auf Inline-Editoren) definieren und
  **ankündigen**, damit andere Sessions sie übernehmen.
- Event-Tabelle (`AdminEventsComponent`): Spalten **„Bisherige Einlässe" und „Verkaufte Spieltickets"
  tauschen**; **Tooltip** mit Erklärung für „Erwartete Zutritte".

### S4 — Scanning-UX + Zugang (`feature/s4-r6-scanning`)

- Event-Auswahl beim Scannen: **Anlässe von heute optisch hervorheben.** Wählt jemand einen Anlass, der
  nicht heute stattfindet, **Rückfrage** („Dieser Anlass ist nicht heute. Bist du sicher?") via
  `ConfirmDialog`, danach **trotzdem zulassen**.
- Zugangspasswort `stockschlag` **auch per Query-Parameter** übergeben können (zusätzlich zum Formular,
  z. B. `?key=stockschlag`), damit ein vorbereiteter Link direkt einloggt.

### S5 — Helfer-Rework (`feature/s5-r6-helpers`)

- **Neuen Helfer per Overlay** erfassen (nicht mehr inline/integriert), Muster wie andere Erstell-Overlays.
- Spalte/Ansicht **„Scans pro Anlass" entfernen**.
- Stattdessen **Icon-Spalte** je Helfer, die ein Overlay öffnet mit der Übersicht, an welchen Anlässen
  dieser Helfer wie viel gescannt hat — **Einlässe und Ausgänge separat** (aus den Visit-Logs).

### S6 — Public-Auslieferung & Härtung (`feature/s6-r6-frontend-hardening`)

Alleiniger Owner von `Program.cs`, `appsettings*.json`, Views/CSS, PDF-/Mail-Templates und dem
Warmup-Teil von `deploy.yml` (keine andere Session fasst diese Dateien an → konfliktfrei).

- **Breiten angleichen:** Saison-Übersicht (`/seasons/`) und Saison-Detail wirken unterschiedlich
  (Anlässe horizontal, Saison vertikal, andere Gesamtbreite). Container-Breite auf ALLEN öffentlichen
  Seiten prüfen und auf einen gemeinsamen Wert vereinheitlichen.
- **Eigene 404-Seite:** Bild `C:\Users\Jan.Haug\Downloads\Schiedsrichterin.png` (nach `wwwroot` kopieren),
  Titel „404 – Kein Tor!", Text (Schiedsrichter-Motto, siehe Auftrag), Button 1 „Zurück auf die Bank"
  (Startseite), Button 2 „Protest einlegen" (Kontakt). Über `UseStatusCodePages…`/Fehlerseiten-Route.
- **PDF-Ticket:** fehlendes **Red-Ants-Logo** ergänzen. **E-Mail:** Logo-Grösse auf sinnvolles Maass
  begrenzen (aktuell zu gross).
- **Security-Header** (Middleware, prod scharf, Backoffice nicht brechen): `X-Frame-Options`,
  `X-Content-Type-Options: nosniff`, `Strict-Transport-Security` (HTTPS), `Content-Security-Policy`
  (konservativ/ggf. nur Public; Backoffice + `/heute/embed` `frame-ancestors` beachten), `Server`-Header
  entfernen (`AddServerHeader=false`).
- **RuntimeMode=Production** für prod (`Umbraco:CMS:RuntimeMode`), dev bleibt Development.
  **Notification-From-E-Mail** von `your@email.here` auf echte Adresse. **Imaging-HMAC:**
  `Umbraco:CMS:Imaging:HMACSecretKey` (Secret/Config).
- **Warmup erweitern:** interne Warmup-Seite/Endpoint (z. B. `/warmup`), die serverseitig mehrere
  Kernseiten vorlädt (Home, `/seasons/`, ein Saison-Detail, `/tickets`, Admin-Login), damit die
  Razor-Runtime-Kompilierung vorgewärmt ist; den GitHub-Warmup-Step `/warmup` statt nur `/` aufrufen lassen.
