# Session-Koordination v2 (Worktrees + Branches) — Runde 4: Payrexx

> Worktree-Modell wie gehabt (`C:\development\RedAnts-s<N>`, eigener Branch je Session).
> Runde 1–3 sind komplett in `main`. Diese Runde integriert die echte Zahlung über
> **Payrexx** (https://docs.payrexx.com/developer). Aktuell ist der Checkout Pseudo:
> `CheckoutController` legt die Bestellung an, ruft sofort `MarkPaid()` und stellt Tickets aus.

## Arbeitsmodell

Runde-4-Branch vom aktuellen `main` anlegen; frei arbeiten; sofort committen; Schema-/Contract-
Änderungen im Änderungs-Log ankündigen; vor Merge auf `main` rebasen, Release-Build grün,
mergen, pushen; `main` bleibt immer grün. Keine Kommentare im Code, Enums als int, keine
Secrets im Code, kein Co-Authored-By. Admin-Bausteine wiederverwenden.

## Zielbild (Architektur)

Payrexx **Gateway API** (gehostete Bezahlseite): Kauf → Bestellung als **Draft** + Warenkorb-
Positionen persistiert → Redirect auf die Payrexx-`link`-Seite → Kunde bezahlt (Karte/TWINT) →
**Webhook** (Server-zu-Server) meldet den Transaktionsstatus → bei `confirmed`: Bestellung
`MarkPaid()`, **Tickets/Karten werden erst jetzt ausgestellt** (aus den persistierten
Positionen), Mail versandt. Rückseite `/kasse/erfolg` zeigt „Zahlung wird verarbeitet" und
pollt den Bestellstatus; `/kasse/abbruch` bei Abbruch. Tickets entstehen also **nie vor
bestätigter Zahlung**. Twint läuft ebenfalls über die Payrexx-Gateway (als Payment-Methode).

**Fallback (wichtig):** Solange `Payrexx:ApiSecret` NICHT gesetzt ist (lokal/Testphase),
behält der Checkout das bisherige Pseudo-Verhalten (sofort bezahlt), damit nichts bricht,
bevor die Keys da sind. Analog zum Turnstile-`Enabled`-Muster.

## Reihenfolge / Abhängigkeiten

Fundament zuerst und früh mergen: **S2 (Payrexx-Client)** und **S4 (Order-Positionen)** sind
unabhängig voneinander. Darauf bauen **S3 (Checkout-Umbau)** und **S5 (Webhook/Fulfillment)**.
**S6 (Admin/Bestellungen)** hängt an S5. **S1 (Konfig/Secrets/Infra/Test)** ist querschnittlich
und schliesst ab (Sandbox-Test, Webhook-Registrierung, Gate-Ausnahme).

## Aufgabenpakete Runde 4

| Session | Branch | Paket | Status |
|---|---|---|---|
| S1 | `feature/s1-r4-payrexx-infra` | Secrets/Config, Gate-Ausnahme Webhook, Sandbox-E2E-Test, Doku | offen |
| S2 | `feature/s2-r4-payrexx-client` | Payrexx-REST-Client (signiert): Gateway erstellen/abfragen, Refund | offen |
| S3 | `feature/s3-r4-checkout-payrexx` | Checkout-Umbau: Draft + Redirect + Rückseiten | Rückseiten in main (`/kasse/erfolg`+Status-Polling, `/kasse/abbruch`); Draft/Redirect-Kern wartet auf S2+S4 |
| S4 | `feature/s4-r4-order-items` | Order-Positionen persistieren (Fulfillment-Grundlage) | offen |
| S5 | `feature/s5-r4-payrexx-webhook` | Webhook + Fulfillment (Tickets ausstellen nach `confirmed`) | offen |
| S6 | `feature/s6-r4-orders-payrexx` | Admin: Provider-Status/Transaktion an der Bestellung, Refund | offen |

---

### S2 — Payrexx-Client (`feature/s2-r4-payrexx-client`) — Fundament

- Infrastructure-REST-Client gegen `https://api.payrexx.com/v1.0/`. Auth: Query-Param
  `instance=<Instance>`, jede Anfrage signiert mit **`ApiSignature` = base64(HMAC-SHA256(
  querystring, ApiSecret))** (auch bei leerem Body die leere Signatur). Config
  `Payrexx:Instance`/`Payrexx:ApiSecret` (+ optional `Payrexx:BaseUrl` für Sandbox).
- Port `IPayrexxGateway`:
  - `CreateGatewayAsync(amountRappen, "CHF", referenceId=OrderNumber, purpose, successUrl,
    failedUrl, cancelUrl, pm[], prefill email/vorname/nachname)` → `{ GatewayId, Link }`.
  - `RetrieveGatewayAsync(gatewayId)` / `RetrieveTransactionAsync(id)` (Status verifizieren,
    dem Webhook-Body nie blind vertrauen).
  - `RefundTransactionAsync(transactionId, amountRappen?)`.
- Statuswerte kapseln (`waiting/confirmed/authorized/cancelled/declined/refunded/…`),
  `Paid = confirmed`. Bei fehlendem `ApiSecret`: `Enabled = false`.

### S4 — Order-Positionen (`feature/s4-r4-order-items`) — Fundament

- Neue Tabelle **`OrderItems`** (`OrderId`, `Kind` int [EventTicket/SeasonPass], `EventId`,
  `SeasonId`, `Category` int, `Quantity`, `UnitPrice`, `EventName`, `CategoryName`;
  additiv via `EnsureTable`). Domäne `OrderItem`, Port `IOrderItems`
  (`SaveAsync(items)`, `GetByOrderAsync(orderId)`). Damit ist eine Draft-Bestellung
  serverseitig (aus dem Webhook, ohne Session) erfüllbar.

### S3 — Checkout-Umbau (`feature/s3-r4-checkout-payrexx`) — nach S2+S4

- `CheckoutController` (regulär UND Express): Bestellung als **Draft** anlegen, Positionen via
  `IOrderItems` persistieren, **KEINE** sofortige `MarkPaid()`/Ticketausstellung mehr. Payrexx-
  Gateway erstellen (`IPayrexxGateway`, S2), auf `Link` redirecten. Gateway-Id an der Bestellung
  ablegen (S6-Feld nutzen/ankündigen).
- Rückseiten: `/kasse/erfolg?order=…` (zeigt „Zahlung wird verarbeitet", pollt Bestellstatus
  bis `Paid` → dann Tickets/QR wie bisher) und `/kasse/abbruch`.
- **Fallback**: wenn Payrexx `Enabled == false`, alter Pseudo-Weg (sofort bezahlt + ausstellen).

### S5 — Webhook + Fulfillment (`feature/s5-r4-payrexx-webhook`) — nach S2+S4

- Endpoint **`POST /payrexx/webhook`** (anonym, gate-frei → S1). Transaktion/Gateway via
  `IPayrexxGateway.RetrieveTransactionAsync` verifizieren (nicht dem Body vertrauen), Bestellung
  über `referenceId`/Gateway-Id finden.
- Bei `confirmed`: `MarkPaid()` + `IOrderLog.AppendAsync(Paid, "Payrexx")`, **Tickets/Karten aus
  `IOrderItems` ausstellen** (EventTicket/SeasonPass, Käufer aus Order), Mail versenden.
  **Idempotent** (Doppel-Webhooks: nur ausstellen, wenn Order noch nicht `Paid`; Dedupe über
  Transaktions-Id). Bei `cancelled/declined`: Order `Cancel()` + Log. `refunded`: `Refund()` + Log.

### S6 — Admin/Bestellungen Payrexx (`feature/s6-r4-orders-payrexx`) — nach S5

- Bestellung um **`PayrexxGatewayId`/`PayrexxTransactionId`** (additive Spalten) erweitern; in
  der Bestellungen-Tabelle Provider-Status/Transaktion anzeigen.
- Admin-**Rückerstattung** aus der Bestellung heraus (ruft `IPayrexxGateway.RefundTransactionAsync`,
  loggt via `IOrderLog`). Der bestehende Bezahlstatus-Inline-Edit bleibt (manuelle Korrektur).

### S1 — Konfig/Secrets/Infra/Test (`feature/s1-r4-payrexx-infra`) — querschnittlich

- Payrexx-**Sandbox** + **Prod**: `Payrexx__Instance`/`Payrexx__ApiSecret` als Azure-App-Settings
  (dev+prod), Secrets nie ins Repo.
- **Gate-Ausnahme** für `/payrexx/webhook` in `Program.cs` (`IsExempt`), sonst blockt das Gate
  den Server-zu-Server-Callback.
- **Webhook-URL** im Payrexx-Dashboard registrieren (`https://tickets.redants.ch/payrexx/webhook`
  bzw. dev). End-to-End-Test im Sandbox (Kauf → Bezahlung → Webhook → Tickets/Mail). In
  `deploy/README.md` dokumentieren.

## Vom Nutzer benötigt (blockierend für Live)

1. **Payrexx-Konto** (Sandbox zuerst, dann Prod): **Instance-Name** + **API-Secret**
   (Payrexx-Dashboard → API & Webhooks). Payment-Methoden (Karte, TWINT) im Dashboard aktivieren.
2. Zugang, um die **Webhook-URL** im Dashboard zu hinterlegen (oder du trägst sie ein).
3. Bestätigung: Währung CHF, MWST 0 (wie gehabt), Rückerstattungen laufen über den Admin.

Bis die Keys da sind, wird gegen die Sandbox gebaut; ohne `ApiSecret` bleibt der Pseudo-Checkout
aktiv, `main` bleibt deploybar.

---

## Änderungs-Log (Schema/Contracts, die andere betreffen)

Neueste zuerst. Nur Änderungen eintragen, die andere Sessions betreffen.

| Datum | Session | Was | Auswirkung |
|---|---|---|---|
| 21.07.2026 | S2 | **Helfer-Zugang für den Scanner (in `main`, `5c9911a`).** Neue additive Tabelle **`Helpers`** (`SeasonId`, `FirstName?`, `LastName?`, `Email?`, `Password`, `Active`, `CreatedAt`; via `EnsureTable`). Neuer Port **`IHelpers`** + `HelperRepository` (Zwei-Wort-Passwort-Generator, global eindeutig). Neuer Admin-Tab **„Helfer"** (pro Saison: erfassen, Passwort/Login-Link kopieren, aktiv/löschen) + Report **`IHelperScanReport`** (Scans je Person pro Anlass, inkl. Auslässe). **Scanner-Auth in `Program.cs`**: neues Cookie `RedAnts.Helper` (DataProtection `RedAnts.HelperSession.v1`), Helfer-Middleware VOR dem Gate; `/scanntickets` verlangt Helfer-Login (`/scan/login` = Passwort ohne Benutzername, `/scan/{passwort}`-Link, `/scan/logout`); `/scanntickets` + `/scan` neu in `IsExempt`. `ScanTickets.cshtml` reicht den Helfernamen als `param-HelperName` an `TicketScanner` (überspringt Namenseingabe, Name = Scan-Operator). Nur aktiv, wenn `BasicAuth:Password` gesetzt ist (Dev unverändert). Optional Config `Scanner:PublicUrl` für den Login-Link. | Wer an `Program.cs`/Gate arbeitet (S1): Helfer-Middleware + neue `IsExempt`-Einträge beibehalten. Nur additive Tabelle. |
| 20.07.2026 | S2 | **Verkaufskontingent für Einzeltickets pro Anlass + Saison-Default (in `main`, `f6bb097`).** Neue additive Spalte **`SeasonPrices.DefaultTicketSalesQuota`** (nullable int; Migration `seasonprices-default-ticket-sales-quota`). `SeasonPrice` hat neu Feld `DefaultTicketSalesQuota` (optionaler letzter Parameter in `Create`/`FromPersistence`). Neuer schmaler Port **`IEventSalesQuota`** (`GetAllAsync`/`SetAsync`) + `EventSalesQuotaEditor` (schreibt `EventPrices.TotalSalesQuota`). Anlässe-Admin: neue Inline-Spalte „Verkaufskontingent"; Saisons-Admin: neue Inline-Spalte „Standard-Verkaufskontingent". `EventPriceDefaults` kopiert neu `SeasonPrice.DefaultTicketSalesQuota` → `EventPrice.TotalSalesQuota` bei neuen Anlässen (bestehendes Mapping `SeasonPrice.TotalSalesQuota` → `EventPrice.AdmissionQuota` unverändert). | Wer `SeasonPrice.Create/FromPersistence` beim Aktualisieren eines bestehenden Preises aufruft: neuen optionalen `defaultTicketSalesQuota` mitgeben, sonst wird er auf null gesetzt. Nur additive Spalte. |
| 20.07.2026 | S4 | **Zentrale Preisstufen pro Saison (Branch `feature/season-price-tiers`).** Neue Tabelle **`SeasonPriceTiers`** (`Id`, `SeasonId`, `Name`, `MaxAge?`, `PromoOfTierId?`, `SortOrder`, `LegacyCategory?`); additive Spalte **`TierId`** (int null) auf `EventPriceCategories`, `SeasonPriceCategories`, `EventTickets`, `SeasonSingleTickets`, `SeasonPasses`, `OrderAddOns`. Verkauf/Preise/Anzeige laufen jetzt über die Stufen-Id; das `TicketCategory`-Enum bleibt als Platzhalter bestehen (neue Direktverkäufe schreiben `Category=0` + echte `TierId`). Idempotenter Startup-Seeder `PriceTierSeeder` legt pro Saison 5 Standardstufen an (Erwachsen/Jugend/Kind + 2 Aktionen) und backfillt `TierId` auf Preis- und Verkaufszeilen (Enum→Stufe via `LegacyCategory`). **Contract-Änderungen:** `AvailableTicketCategory` ist jetzt `(int TierId, string Name, decimal Price, bool Available, int? Remaining, DateOnly? AvailableUntil)` (kein `Category`-Enum mehr); `TicketDemand`/`PassDemand` tragen `TierId` statt `Category`; `IEventPricing.FindAvailableAsync`→`FindAvailableByTierAsync(eventId, tierId)`, `ISeasonPassPricing` neu `FindAvailableByTierAsync` + `GetSoldCountsAsync` liefert `IReadOnlyDictionary<int,int>` (Stufen-Id); neuer Port **`IPriceTiers`**; `ICartService.Add`/`AddSeasonPass` nehmen `int tierId` statt `TicketCategory`; `CartItem.Category`→`TierId`, `CartItem.Key` enthält `TierId`; `FulfillmentItem`/`FulfillmentAddOn` tragen `TierId`; `OrderAddOnLine` + `SeasonPassListItem` + `IssuedTicket` haben zusätzlich `TierId`/`CategoryName`. Bundles/CSV-Import/manuelle Saisonkarten-Erfassung bleiben vorerst auf dem Enum (Tickets werden per Backfill einer Stufe zugeordnet). | Wer `IEventPricing`/`ISeasonPassPricing`/`ICartService` mockt oder `AvailableTicketCategory`/`TicketDemand`/`PassDemand`/`FulfillmentItem`/`OrderAddOnLine` konstruiert: neue Signaturen. Wer `SeasonPass.Create`/`EventTicket.Create`/`SeasonSingleTicket.Create` aufruft: optionaler `tierId`-Parameter am Ende. Nur additive Tabellen/Spalten. |
| 20.07.2026 | S2 | **Scanner-Selbsttest-QR (in `main`, `fac233e`).** Neuer Enum-Wert **`AdmissionOutcome.Test`**. `AdmissionService.ScanTicketAsync` erkennt einen Test-Token (Ticket-Token mit `uuid == Guid.Empty`) und liefert ohne DB-Zugriff das Ergebnis „Test erfolgreich" (Scanner zeigt es türkis). Öffentliche, gate-freie Seite **`/scanner-test`** (`ScannerTestController` + `Views/ScannerTest.cshtml`) zeigt den Test-QR (kodiert `…/ticket/{Test-Token}` wie echte Tickets). | Wer erschöpfend über `AdmissionOutcome` schaltet: neuen Wert `Test` behandeln (`ScanOutcome.Ok` ist bei Test true). Reservierte Test-UUID `Guid.Empty` nicht für echte Tickets verwenden. Kein DB-Schema. |
| 20.07.2026 | S2 | **Express-Regel + MwSt-Hinweis + rechtliche Content-Seiten (in `main`, `cd55d91` + `b996df3`).** (1) Express-Checkout „Direkt kaufen" nur noch bei Total < 50 CHF **und** ohne Saisonkarte im Warenkorb; sonst Pflicht-Checkout mit Adresse. Neue Policy **`ExpressCheckout.IsAllowed(cart)`** (Cart-Namespace); `Express()`/`ExpressPay()`/`CartController.AddAndCheckout` leiten sonst auf `/kasse`. (2) MwSt-Hinweis auf Bezahl- und Express-Seite. (3) Neuer Doctype **`legalPage`** (Titel/Slug/Bild/RichText) + Template `LegalPage.cshtml`; zwei geseedete, backoffice-editierbare Knoten **Impressum** (`/impressum`) und **Datenschutz** (`/datenschutz`), geroutet über `LegalPageContentFinder` (per Slug). `LegalController` + `Views/Datenschutz.cshtml` **entfernt**; Gate-Ausnahme für `/impressum` + `/datenschutz`; Footer mit Impressum-Link. | S3 (Checkout-Umbau): `ExpressCheckout.IsAllowed`-Gate beibehalten. `/datenschutz` ist neu ein Content-Knoten statt MVC-Route (Slug `datenschutz` nicht umbenennen). Kein DB-Schema, nur Umbraco-Content. |
| 11.07.2026 | S2 | **Saisonkarten-Zusatzoptionen (in `main`, `9a7e925`).** Zwei neue additive Tabellen: **`SeasonAddOns`** (`SeasonId`, `Label`, `Price`, `Active`, `SortOrder`) und **`OrderAddOns`** (`OrderId`, `SeasonId`, `SeasonName`, `Category` int, `CategoryName`, `Label`, `Price`, `Quantity`); beide via `EnsureTable`. Neue Ports **`ISeasonAddOns`** (`GetBySeasonAsync`/`ReplaceForSeasonAsync`), **`IOrderAddOns`** (`SaveAsync`/`GetByOrderAsync`), **`IAddOnNotifier`** (Mail an tickets@redants.ch); Domäne `SeasonAddOn`, Record `OrderAddOnLine`. **`ICartService.AddSeasonPass` hat neu einen Parameter `IReadOnlyList<CartAddOn> addOns`**; `CartItem` hat neu `AddOns` und der `Key` enthält jetzt die Add-on-Ids. `CartController.AddSeasonPass` nimmt `int[]? addOns`. `CheckoutController` hat zwei neue Ctor-Abhängigkeiten (`IOrderAddOns`, `IAddOnNotifier`); persistiert gewählte Optionen und benachrichtigt den Admin nach dem Kauf. Zusatzoptionen werden im Admin-Bereich **Saisons** (Aktions-Button „＋") erfasst. | S3 (Checkout-Umbau): Add-on-Persistenz + Admin-Mail beim Fulfillment mitnehmen (aktuell in `FinalizeOrderAsync`). Wer `ICartService` implementiert/mockt: neuer `addOns`-Parameter. Wer `CartItem.Key` annimmt: enthält jetzt Add-on-Ids. Nur additive Tabellen. |
| 10.07.2026 | S2 | **Pflicht-Bestätigung Datenschutzerklärung im Checkout (in `main`, `cdee839`).** `Pay` und `ExpressPay` haben neu den Parameter `acceptPrivacy`; ohne Bestätigung wird der Kauf abgelehnt (Fehlermeldung, keine Bestellung). Auf der Bezahlseite und in der Express-Kasse eine `required`-Checkbox mit Link auf `/datenschutz` (Seite von S1). | S3 (Checkout-Umbau): den `acceptPrivacy`-Gate vor dem Anlegen des Drafts beibehalten. Kein Schema. |
| 10.07.2026 | S2 | **Newsletter-Opt-in (in `main`, `4f49268`).** Neue additive Tabelle **`NewsletterSignups`** (`Email`, `Name?`, `Source`, `SignedUpAt`, `Status` int [Pending/Transferred], `TransferredAt?`; via `EnsureTable` in `CreateTicketingSchema`). Neuer Port **`INewsletterSignups`** (`SubscribeAsync`/`GetAllAsync`/`SetTransferStatusAsync`), Domäne `NewsletterSignup` + Enum `NewsletterTransferStatus`. `CheckoutForm` hat neu `AcceptNewsletter` (bool); `ExpressPay` hat neu Parameter `acceptNewsletter`; `FinalizeOrderAsync` hat zwei neue Parameter (`subscribeNewsletter`, `newsletterSource`). Neuer Admin-Tab **„Newsletter"** mit Inline-Status-Umschaltung (offen/übertragen). | S3 (Checkout-Umbau): die neuen Parameter beibehalten und das Opt-in in den Draft-Weg übernehmen (Anmeldung ist unabhängig vom Zahlungsergebnis). Wer `INewsletterSignups` mockt: drei Methoden. Nur additive Tabelle. |
| 06.07.2026 | S1 | Runde-4-Plan (Payrexx) verteilt (dieses Dokument). | Alle: Runde-4-Branch anlegen; S2/S4 zuerst mergen. |
| 06.07.2026 | S6 | **Bestellungen-Log + Bezahlstatus (in `main`).** Neue Tabelle **`OrderStatusLogs`** (`OrderId`, `ToStatus` int, `ChangedBy`, `OccurredAt`, `Note?`; additiv via `EnsureTable`). Neuer Port **`IOrderLog`** (`AppendAsync`/`GetByOrderAsync`) + Record `OrderLogEntry`; `IOrders` hat neu **`GetByIdAsync(int)`**; neuer `IOrderStatusEditor` (wendet `MarkPaid`/`Cancel`/`Refund` an und loggt mit `AdminIdentity`). `OrderListItem` hat neu ein erstes Feld **`int OrderId`**. `CheckoutController.FinalizeOrderAsync` loggt Kauf (Draft) + Bezahlt. | Wer `IOrders` implementiert/mockt: neue `GetByIdAsync`. Wer `OrderListItem` konstruiert: neues erstes Feld `OrderId`. Nur additive Tabelle. |
| 06.07.2026 | S5 | **Content-Struktur konsolidiert (in main, `561c568`).** Der doppelte „Saisons"-Baum ist weg (`saisonsPromo`-Root-Knoten + Doctype entfernt; Header/Bild/Text auf den `seasonsFolder`-Hauptknoten migriert). `seasonsFolder` hat neu `headerText`/`headerImage`/`bodyText` + `SaisonsPromo`-Template. Neuer `SaisonsContentFinder` routet `/saisons/`. Event-„Ort" nutzt einen eingeschränkten Venue-ContentPicker. | Alias `SaisonsPromoType`/-Doctype/-Knoten existieren nicht mehr; `/saisons/` bleibt als Route. Kein DB-Schema. |
| 06.07.2026 | S4 | **Saisonkarten CSV-Import/Export + Bundle (in `main`, `0ef3bac`):** additive Spalte `SeasonPasses.Reference`; `SeasonPass.Create/FromPersistence` mit optionalem `reference`; Port `ISeasonPasses.ImportAsync(...)` + Records; `SeasonPassListItem.Reference`; Endpunkte `/admin/saisonkarten/beispiel.csv` + `.../passes.csv?bundle=`. | Wer `ISeasonPasses` mockt: neue `ImportAsync`. `SeasonPass.Create`-`reference` optional. Nur additive nullable Spalte. |
| 06.07.2026 | S1 | **Kanonische Ticket-URL (in `main`):** Service `IPublicBaseUrl.Resolve(request)` (Config `Tickets:PublicBaseUrl`, Azure prod=`https://tickets.redants.ch`/dev=`https://tickets-dev.redants.ch`, Fallback Request-Host). Alle absoluten Ticket-URLs/QRs bauen darüber statt `Request.Host`. | Wer Ticket-Links/QRs baut: `IPublicBaseUrl` injizieren. Kein Schema/Enum. |
| 05.07.2026 | S1 | Gate-Ausnahmen (`Program.cs` `IsExempt`): `/App_Plugins`, `/css`, `/js`, `/lib`, `scanner-sw.js`, `site.webmanifest`. | Wer am Gate arbeitet: neue Assets/Pfade in `IsExempt` aufnehmen. |
| 05.07.2026 | S1 | `FreeEntryType.Child` (int 4); getrennte Saison-Angebote `PassOffered`/`PassQuota` + `TicketOffered`/`TicketQuota` (`SeasonCategoryPrice`); `EventPriceDefaults`; `TicketEventFreeEntryQuotas`; Abendkasse-Express; Mail „Ticket X von Y". | Wer `SeasonCategoryPrice`/`IAdmissionService`/`FreeEntryType` nutzt: neue Signaturen/Werte. |
