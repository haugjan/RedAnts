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
| S2 | `feature/s2-r5-addon-texts` | Zusatzoptionen: Vorabinfo (Info-Icon-Tooltip) + Nachkauf-Info (Bestätigung UND Mail) | erledigt, in main gemerged (`4e305c4`; auf S1 rebased, additiv an `FulfillAsync`/Snapshot eingehängt; App-Boot gegen Dev-DB verifiziert = `/saisons/` 200 mit Zusatzoptionen, Migration `seasonaddons-info-texts`, Build grün, 93 Domain-Tests grün) |
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
| 21.07.2026 | S2 | **Zusatzoptionen-Texte (in `main`, `4e305c4`).** `SeasonAddOns` bekommt zwei additive Textspalten **`InfoBeforePurchase`** (Vorabinfo) + **`InfoAfterPurchase`** (Nachkauf-Info); Migration `seasonaddons-info-texts`; `SeasonAddOn.Create/FromPersistence` haben zwei neue optionale End-Parameter. Admin-Zusatzoptionen-Modal (`AdminSeasonsComponent`) hat zwei neue Textarea-Spalten; `SaisonsPromo` zeigt die Vorabinfo als Info-Icon-Tooltip. **`FulfillmentAddOn` hat neu ein erstes Feld `int Id`** (Snapshot; alte Drafts ohne `Id` deserialisieren tolerant zu `Id=0`). `CheckoutController.FulfillAsync` gibt neu `(Tickets, AddOnInfos)` zurück; `CheckoutConfirmationView` hat neu `AddOnInfoTexts`. **`OrderMailModel` hat neu optionalen letzten Parameter `AddOnInfoTexts`**; `OrderMailer` rendert die Nachkauf-Info additiv (koexistiert mit S3s `IWalletPass`), `Confirmation.cshtml` zeigt sie ebenfalls. | Wer `OrderMailModel` konstruiert: neuer optionaler letzter Parameter (Default null). Wer `FulfillmentAddOn` konstruiert: neues erstes Feld `Id`. Wer `SeasonAddOn.Create/FromPersistence` beim Aktualisieren aufruft: neue optionale Info-Parameter. Nur additive Spalten. |
| 21.07.2026 | S1 | **OrderItems-Fundament (in `main`, `c98236f`).** Neue Tabelle **`OrderItems`** (`OrderId`, `Kind` int, `ArticleGuid` guid?, `RefId`, `Category` int, `Label`, `Quantity`, `UnitPrice`) + Domäne `OrderItem` + Enum `OrderItemKind` (EventTicket/SeasonSingle/SeasonPass/MemberCard/AddOn) + Port **`IOrderItems`** (`SaveAsync(orderId, items)`, `GetByOrderAsync`). Additive **`ArticleGuid`** (nullable) auf `EventPriceCategories`/`SeasonPriceCategories`/`SeasonAddOns` mit idempotentem `NEWID()`-Backfill. Neuer Enum-Wert **`PaymentMethod.Manual`** (int 4, angehängt). Neuer Port **`IAdminOrderFactory`** (`CreateAsync(buyer, email, lines, createdBy)` → legt Order (Manual/Paid) + OrderItems + Order-Log an); alle 4 Admin-Create-Wege (Einzelticket, Einzel-Saisonkarte, Mitgliederkarte, Flex-/Event-Bundle) legen jetzt darüber eine Bestellung an. **`FulfillAsync` schreibt OrderItems**; **Gratis-Skip**: Payrexx nur bei `payrexx.Enabled && saved.TotalGross > 0m`. Positionen-Overlay `OrderItemsOverlay` in `AdminOrdersComponent`. | S2 baut additiv auf `FulfillAsync`/`FulfillmentSnapshot` auf (jetzt rebasen). Wer `PaymentMethod` als int mappt: Wert 4 = Manual. Wer die 3 Katalog-Records baut: neue nullable `ArticleGuid`-Spalte. **Follow-up offen:** Ticket-`OrderId`-Backlink für Mitgliederkarten/Bundles + Saisonkarten-Import-Order (Positionen sind erfasst, aber die erzeugten Karten/Bundle-Tickets tragen die `OrderId` noch nicht). |
| 21.07.2026 | S1 | **OrderId-Backlink nachgezogen (in `main`, `93a8318`).** `EventTicket.CreateForBundle`/`SeasonSingleTicket.CreateForBundle` + Ports `IMemberCards.CreateAsync`/`IFlexTicketBundles.CreateAsync`/`IEventTicketBundles.CreateAsync` erhalten optionalen `int? orderId = null`; die Repos setzen ihn auf die Ticket-Zeile (`OrderId`). Die 3 Admin-Wege (Mitgliederkarte, Flex-Bundle, Ticket-Bundle) fangen die erzeugte Order und reichen `order.Id` durch. Damit tragen ALLE backoffice-erzeugten Tickets/Karten die `OrderId`. | Rein additiv (neue optionale Parameter am Ende der Signaturen), kein Umbau bestehender Call-Sites nötig. Rest-Follow-up: Saisonkarten-CSV-Import legt noch keine Order an (`MemberCardRepository.ImportAsync`, offen). |
| 21.07.2026 | S6 | **Public-Slim + Format + Fixes (in `main`, `c639985`).** Neue öffentliche Route **`/heute/embed`** rendert ein schlankes iFrame-Widget (`Views/NextEventEmbed.cshtml`, `Layout = null`, `NextEventEmbedModel` in `Features/Ticketing/Public`) für den nächsten öffentlichen Anlass, sendet `Content-Security-Policy: frame-ancestors *` und verlinkt per `target="_top"` auf die absolute Kauf-URL. **CSV-QR-Spalte vereinheitlicht:** `MemberExportController` heisst die Ticket-URL-Spalte jetzt `Link` (vorher `QrUrl`) und schreibt `\r\n`, damit alle vier Exporte (`Event`/`Flex`/`SeasonPass`/`Member`) dieselbe QR-taugliche `Link`-Spalte tragen. Venue-Picker-Fix in `TicketingContentTypeSeeder.EnsureVenuePicker` (schreibt die Ordner-Key-GUID statt der UDI in `startNodeId`). Mitglieder-Geburtsdatum nutzt jetzt `<DateBox>` (dd.MM.yyyy). | Falls jemand einen weiteren QR-/Ticket-Export ergänzt: Spalte `Link` nennen (nicht `QrUrl`). Neue Route `/heute/embed` und View `NextEventEmbed.cshtml` sind reserviert. Venue-Fix ist reine DB-Config, kein Code-Contract. |
| 21.07.2026 | S3 | **Wallet/PDF-Auslieferung (in `main`, `8cf0796`).** Neue Ports `ITicketPdf` (QuestPDF) + `IWalletPass` (Google-Wallet-Save-JWT, config-gated `GoogleWallet:IssuerId`/`ServiceAccountJson`), registriert in `TicketDeliveryComposer` (setzt QuestPDF-Community-Lizenz). Neue Endpunkte `/ticket/{token}/pdf` und `/ticket/{token}/wallet`; `WebTicketViewModel` hat neu `Token` + `WalletEnabled`. **`OrderMailer` injiziert neu `IWalletPass`** und zeigt pro Ticket PDF-/Wallet-Links. Neue NuGet `QuestPDF`. | Wer `OrderMailer` konstruiert/mockt: neuer Ctor-Parameter `IWalletPass`. Wer `WebTicketViewModel` baut: zwei neue optionale Felder. S2 (Add-on-Nachkauftext in `OrderMailer`) additiv einhängen. Kein DB-Schema. |
| 21.07.2026 | S4 | **Event/Saison-Admin-Tabellen (in `main`, `591c250`).** `EventAdmissionCounts` hat zwei neue optionale Felder `SeasonPassHolders`/`MemberHolders` + berechnete Property `ExpectedAdmissions` (= verkaufte Spieltickets + Saisonkartenbesitzer + Mitglieder + bisherige Freieinlässe); `EventAdmissionReportReader` injiziert neu `IEvents` (Anlass→Saison-Zuordnung für die Bestandszahlen). Neue wiederverwendbare Komponente **`LinkOverlay.razor`** (öffentlicher + interner Link mit Kopier-Button + statusabhängige Hinweise). `AdminEventsComponent`: neue Spalte „Erwartete Zutritte" (rot bei Überschreitung Einlasskontingent, oranger Hinweis bei potenzieller Überbuchung), „Bisherige Einlässe" rot/orange; Link-Spalte (Events **und** Seasons) öffnet jetzt das `LinkOverlay` statt Direktkopie. | Wer `EventAdmissionCounts` konstruiert: zwei neue optionale Felder (Default 0). Wer `IEventAdmissionReport` implementiert/mockt bzw. `EventAdmissionReportReader` ersetzt: Reader braucht `IEvents`. Neuer Admin-Baustein `LinkOverlay` steht anderen Sessions zur Verfügung. Kein DB-Schema. |
| 21.07.2026 | S5 | `TicketingAdminState` ist jetzt beobachtbar (`event Changed`) und trägt zusätzlich `SelectedEventId` + `SelectedBundleId`. `SelectedSeasonId` etc. sind Properties, die `Changed` auslösen. `TicketingAdminComponent` spiegelt Tab/Saison/Anlass/Bundle in die URL-Query. | Wer `TicketingAdminState` neu nutzt: Property-Semantik (löst `Changed` aus), rein additiv. `InlineEditBase` schliesst Editiermodus zusätzlich per Blur (`HandleBlur`) und hat neu `EditorElement` (Auto-Fokus). Keine Signaturänderung an den Inline-Editoren. |
| 21.07.2026 | S1 | Runde-5-Plan verteilt (dieses Dokument). | Alle: Runde-5-Branch anlegen; S1 zuerst mergen, S2/S3 danach rebasen. |
