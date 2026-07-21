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
| S1 | `feature/s1-r5-order-items` | Artikel-GUID + `OrderItems`-Tabelle; Bestellung+Positionen bei jeder Ticket-Erzeugung (inkl. Admin); Positionen-Overlay in Bestellungen; Gratis-only überspringt Payrexx | offen |
| S2 | `feature/s2-r5-addon-texts` | Zusatzoptionen: Vorabinfo (Info-Icon-Tooltip) + Nachkauf-Info (Bestätigung UND Mail) | offen |
| S3 | `feature/s3-r5-wallet-pdf` | Google Wallet + PDF; Download-Links auf Ticket-Seite und in der Bestätigungsmail | offen |
| S4 | `feature/s4-r5-event-columns-links` | Event-Tabelle: neue Summenspalte + Einfärbungen; Link-Kopier-Overlay (Events + Seasons) | offen |
| S5 | `feature/s5-r5-admin-infra` | Inline-Edit auch per Mausklick verlassen; Admin-Position (Tab/Saison/Anlass/Bundle) via URL merken | offen |
| S6 | `feature/s6-r5-public-format-fixes` | iFrame-Slim „Nächster Anlass"; restliche Datumsfelder auf dd.MM.yyyy; Venue-Bug „Ort nicht änderbar"; CSV-QR-Spalte prüfen/vereinheitlichen | offen |

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
| 21.07.2026 | S1 | Runde-5-Plan verteilt (dieses Dokument). | Alle: Runde-5-Branch anlegen; S1 zuerst mergen, S2/S3 danach rebasen. |
