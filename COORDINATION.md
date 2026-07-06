# Session-Koordination v2 (Worktrees + Branches) — Runde 3

> Jede Session arbeitet in einem eigenen Git-Worktree (`C:\development\RedAnts-s<N>`) auf einem
> eigenen Branch. Runde 1 (Scanner, Tabellen-Standard, Mail, Kasse, /ticketing/, Bestellungen)
> und Runde 2 (Freieinlass-Kategorien, Inline-Edit, Verkauf-Fixes, Saisonkarten-Kauf,
> Saisons-Preismodell, Anlässe/Mitglieder, Abendkasse-Express) sind komplett in `main`.

## Arbeitsmodell

1. **Runde-3-Branch im bestehenden Worktree anlegen:**
   ```
   git -C C:\development\RedAnts-s<N> fetch origin
   git -C C:\development\RedAnts-s<N> checkout -b <branch> origin/main
   ```
2. Frei arbeiten; sofort und atomar committen, `git add -A` erlaubt.
3. Schema-/Contract-Änderungen (Tabellen, Enums, Port-Signaturen) im Änderungs-Log ankündigen.
4. **Merge nach `main`**: `git fetch` + Rebase auf aktuellen `main`, Release-Build grün
   (`dotnet build -c Release`), dann Merge und Push. `main` bleibt **immer grün**.
5. Deploy: Push `feature/**` → DEV, Push `main` → DEV + PROD.
6. Regeln: keine Kommentare im Code (Rationale → `ARCHITECTURE.md`), Enums als int, Aliase aus
   `*Aliases`, keine Secrets im Code, kein Co-Authored-By. Admin-Bausteine wiederverwenden
   (`ConfirmDialog`, `VisitsOverlay`, `AdminFormat`, `AdminIdentity`, Inline-Edit-Zellen).
7. Statusupdates: nur diese Datei direkt auf `main`; Code nur auf dem Branch.

## Aufgabenpakete Runde 3

| Session | Branch | Paket | Status |
|---|---|---|---|
| S1 | `feature/s1-r3-heute-backups` | `/heute` (nächstes/heutiges Spiel) + DB- und Medien-Backups | **erledigt, in main gemerged** (`/heute` überarbeitet; Azure: prod PITR 14d + LTR 4W/12M, dev PITR 7d + LTR 2W; Storage Soft-Delete 14d + Versionierung auf allen 3 Konten; dokumentiert in deploy/README.md) |
| S2 | `feature/s2-r3-admin-cleanup` | Admin: alle Erklärtexte nach dem Titel entfernen | **erledigt, in main gemerged** (`a83edae`; 5 `ta-page-lead`-Absätze entfernt, Events/Saisons/Spieltickets hatten schon keinen; tote CSS-Regel weg, Titel-Abstand übernommen) |
| S3 | `feature/s3-r3-logos-maps` | Team-Logos fixe Breite + Google-Karte/Link aus dem Ort | offen |
| S4 | `feature/s4-r3-saisonkarten-csv` | Saisonkarten CSV-Import + Export (Bundle-Wahl) | **erledigt, in main gemerged (`0ef3bac`)**: `SeasonPass.Reference`-Bundle + Bundle-Spalte, CSV-Import „Daten importieren (CSV)" mit Beispieldatei (Bundle+Käufer Pflicht, Adresse optional → verknüpfte Order), Export „Saisonkarten exportieren (CSV)" mit Bundle-Wahl |
| S5 | `feature/s5-r3-content-struktur` | Content-Struktur „Saisons" entdoppeln + Ort-Auswahl nur Venues | **erledigt, in main gemerged** (`561c568`; gegen Dev-DB verifiziert: Migration lief, /ticketing/ + /saisons/ je 200, idempotent) |
| S6 | `feature/s6-r3-bestellungen` | Bestellungen (Reihenfolge, Bezahlstatus, Log) + Mitglieder-Spalte/Button | **erledigt, in main gemerged** (`09349e1`): Spaltenreihenfolge, Bezahlstatus inline (Nachfrage), `OrderStatusLogs`+`IOrderLog` mit Log-Overlay, Kauf/Bezahlt/Admin-Hooks; Mitglieder Referenz nach „Erstellt" + Export-Button umbenannt |

Weitgehend unabhängig. Keine harte Reihenfolge; bei `Tabs`-Array/`ticketing-admin.css`/`Program.cs`
additiv auflösen.

---

### S1 — /heute + Backups (`feature/s1-r3-heute-backups`)

- **`/heute` überarbeiten** (`Features/Ticketing/Public/TodayController.cs`): Route führt auf das
  **heutige** Spiel, falls es eines gibt (auch wenn die Startzeit heute bereits vorbei ist, immer
  das heutige, bei mehreren das früheste heute). Gibt es heute keines, auf das **nächste
  kommende** Spiel (frühestes Datum > heute). Nur wenn gar keines existiert, auf `/ticketing/`.
- **DB-Backup** für **beide** Azure-SQL-Datenbanken (`sqldb-redants-dev`, `sqldb-redants-prod`
  auf `sql-redants-ch`): sinnvolle, regelmässige Sicherung einrichten (Long-Term-Retention-
  Policy, z. B. wöchentlich 4 Wochen + monatlich 12 Monate; PITR-Aufbewahrung prod prüfen/
  erhöhen). Per `az` konfigurieren und in `deploy/README.md` dokumentieren.
- **Medien-Backup** für die Blob-Storage-Konten (Umbraco-Media): Soft-Delete + Versionierung
  aktivieren (+ Lifecycle-Retention), sodass gelöschte/überschriebene Medien wiederherstellbar
  sind. Per `az` konfigurieren und dokumentieren.

### S2 — Admin-Cleanup (`feature/s2-r3-admin-cleanup`)

- In **allen** Admin-Tabs die Erklärtexte direkt nach dem Titel entfernen (`<p class="ta-page-lead">…`)
  in `AdminEventsComponent`, `AdminSeasonsComponent`, `AdminTicketsComponent`,
  `AdminFlexTicketsComponent`, `AdminSeasonCardsComponent`, `AdminMemberCardsComponent`,
  `AdminFreeEntriesComponent`, `AdminOrdersComponent`. Nur der Titel bleibt.

### S3 — Team-Logos + Ort/Karte (`feature/s3-r3-logos-maps`)

- **Team-Logos fixe Breite/Höhe**: Heim-/Gegnerlogos in `Views/Partials/Blocks/eventListElement.cshtml`
  und `Views/TicketEvent.cshtml` erscheinen aktuell in Originalgrösse und verzerren das Layout
  (siehe Screenshot). Feste Box (z. B. Höhe 40–48px, Breite fix, `object-fit: contain`) in
  `wwwroot/css/site.css`, damit alle Logos gleich gross wirken.
- **Google-Karte/Link aus dem Ort übernehmen**: Der Ort (`Venue`) hat `googleGeoId`
  (`TicketingAliases.VenueGoogleGeoId`). Statt `venue.Name + ", Winterthur"` in `TicketEvent.cshtml`
  die Google-Daten des Orts nutzen (Karte via Place/Geo-ID einbetten, „Route öffnen"-Link auf
  Google Maps mit derselben ID). Ort-Read-Modell ggf. um die ID erweitern (Catalog-Reader).

### S4 — Saisonkarten CSV (`feature/s4-r3-saisonkarten-csv`)

Analog zum Mitglieder-Import/-Export (`MemberImportController`/`MemberExportController`/`MemberCsv`):
- **Import**: Button **„Daten importieren (CSV)"** im Saisonkarten-Tab, Overlay mit Beispieldatei-
  Download. Pflichtfelder je Zeile: **Bundle** (Referenz) und **Käufername**; übrige Adressfelder
  (Firma/Strasse/PLZ/Ort/Land/E-Mail/Telefon) fakultativ, aber importierbar. Erzeugt je Zeile
  eine Saisonkarte (`SeasonPass`) im gewählten/angegebenen Bundle mit Käufer.
- **Bundle-Spalte** in der Saisonkarten-Tabelle direkt nach „Erstellt" einreihen.
- **Export**: Button **„Saisonkarten exportieren (CSV)"**; beim Klick kann das **Bundle** gewählt
  werden, das exportiert wird (analog Flextickets-Export).
- Saisonkarten-Bundle-Erstellung existiert bereits (Runde 2); Import/Export darauf aufbauen.

### S5 — Content-Struktur (`feature/s5-r3-content-struktur`)

- **„Saisons" entdoppeln**: Aktuell gibt es einen `seasonsFolder`-Knoten „Saisons" (unter
  `ticketingRoot`, enthält Saisons/Anlässe) UND einen separaten `saisonsPromo`-Root-Knoten
  „Saisons" (die Werbeseite). Zwei „Saisons" im Baum sind verwirrend (siehe Screenshot). Der
  Hauptknoten „Saisons" soll alles abbilden; der überflüssige zweite Knoten entfällt. Die Promo-
  Properties (Header/Headerbild/WYSIWYG) an den Hauptknoten hängen, Template + `/saisons/`-Route
  auf diesen einen Knoten, den redundanten Root-Promo-Knoten/Doctype entfernen (Seeder +
  Reader + Routing). Bestehenden Content sauber migrieren; Seeder idempotent halten.
- **Ort-Auswahl bei Events nur Venues**: Die Event-Property „Ort" (`A.EventVenue`) nutzt einen
  generischen `Umbraco.ContentPicker`, der sämtlichen Content anbietet. Auf Venues einschränken
  (Datentyp-Config mit Start-Node = Venues-Ordner bzw. nur `venue`-Doctype; ggf. eigener
  ContentPicker-Datentyp im Seeder).

### S6 — Bestellungen + Mitglieder (`feature/s6-r3-bestellungen`)

Bestellungen (`AdminOrdersComponent` + `OrderAdminReport`/Reader, `Order`-Domäne):
- **Spaltenreihenfolge**: Bestell-Nr., Adresse, Kaufdatum, Bezahlt, Betrag, Spieltickets,
  Saisonkarten.
- **Bezahlstatus editierbar** (Inline-Select mit Nachfrage, S2-Fundament): Draft/Paid/Cancelled/
  Refunded über `Order.MarkPaid()`/`Cancel()`/`Refund()`.
- **Order-Änderungs-Log** (neue Tabelle `OrderStatusLogs`: `OrderId`, `ToStatus`, `ChangedBy`,
  `OccurredAt`, optional `Note`; additiv via `EnsureTable`). Geloggt wird: **Erstellung** (beim
  Kauf), **Bezahlt** (beim erfolgreichen Zahlungs-Rückschluss; heute Pseudo-sofort in
  `CheckoutController` → beide Ereignisse loggen), und **jede Admin-Statusänderung**
  (mit `AdminIdentity` als Person). Contract/Port `IOrderLog.AppendAsync(...)`.
- **Log-Icon** am Zeilenende öffnet ein Overlay mit den Log-Einträgen der Bestellung.

Mitgliederkarten (`AdminMemberCardsComponent`):
- Spalte **„Referenz"** direkt nach „Erstellt" einreihen.
- Export-Button umbenennen in **„Mitglieder exportieren (CSV)"**.

---

## Änderungs-Log (Schema/Contracts, die andere betreffen)

Neueste zuerst. Nur Änderungen eintragen, die andere Sessions betreffen.

| Datum | Session | Was | Auswirkung |
|---|---|---|---|
| 06.07.2026 | S6 | **Bestellungen-Log + Bezahlstatus (Branch `feature/s6-r3-bestellungen`, folgt in `main`).** Neue Tabelle **`OrderStatusLogs`** (`OrderId`, `ToStatus` int, `ChangedBy`, `OccurredAt`, `Note?`; additiv via `EnsureTable` in `CreateTicketingSchema`). Neuer Port **`IOrderLog`** (`AppendAsync`/`GetByOrderAsync`) + Record `OrderLogEntry`; `IOrders` hat neu **`GetByIdAsync(int)`**; neuer `IOrderStatusEditor` (wendet `MarkPaid`/`Cancel`/`Refund` an und loggt mit `AdminIdentity`). `OrderListItem` hat neu ein erstes Feld **`int OrderId`**. `CheckoutController.FinalizeOrderAsync` loggt Kauf (Draft) + Bezahlt (regulär + Express). | Wer `IOrders` implementiert/mockt: neue `GetByIdAsync`. Wer `OrderListItem` konstruiert: neues erstes Feld `OrderId` (nur der Reader). `CheckoutController` nur additiv erweitert (`IOrderLog`-Aufrufe). Kein Enum entfernt; nur additive Tabelle. |
| 06.07.2026 | S5 | **Content-Struktur konsolidiert (in main, `561c568`).** (1) Der doppelte „Saisons"-Baum ist weg: der `saisonsPromo`-Root-Knoten UND der `saisonsPromo`-Doctype werden vom Seeder entfernt (Header/Bild/Text auf den `seasonsFolder`-Hauptknoten migriert, idempotent). Der `seasonsFolder`-Doctype hat neu `headerText`/`headerImage`/`bodyText` + das `SaisonsPromo`-Template. Alias `SaisonsPromoType` entfernt. (2) Neuer `IContentFinder` (`SaisonsContentFinder`) routet `/saisons/` auf den `seasonsFolder`-Knoten; registriert via `SaisonsRoutingComposer`. (3) Event-Property „Ort" nutzt neu einen dedizierten `Umbraco.ContentPicker`-Datentyp „Ticketing Ort (Venues)" mit `startNodeId` = Orte-Ordner (nur Venues wählbar); bestehende `venue`-UDI-Werte bleiben gültig. | **Alle**: `saisonsPromo`-Doctype/-Knoten und der Alias `SaisonsPromoType` existieren nicht mehr; die Promo-Seite lebt am `seasonsFolder`, `/saisons/` bleibt als Route (ContentFinder). Kein DB-Schema (nur Umbraco-Content/Doctypes/Datentypen). Event-`venue`-Alias/-Wert unverändert, nur der Picker ist eingeschränkt. |
| 06.07.2026 | S4 | **Saisonkarten CSV-Import/Export + Bundle (in `main`, `0ef3bac`):** additive Spalte `SeasonPasses.Reference` (NVARCHAR(100) NULL, Migrationsschritt `seasonpasses-reference`); `SeasonPass.Create/FromPersistence` haben neu einen optionalen `reference`-Trailing-Param; `SeasonPassRepository` bekommt `IOrders` injiziert (für optionale Adress-Order beim Import). Port `ISeasonPasses` hat neu `ImportAsync(seasonId, rows, createdByName?, createdByEmail?)` + Records `SeasonPassImportRow`/`SeasonPassImportAddress`. `SeasonPassListItem` um `Reference` erweitert (optional). Neue Endpunkte `/admin/saisonkarten/beispiel.csv` + `/admin/saisonkarten/season/{id}/passes.csv?bundle=` (Bundle-Filter + Bundle-Spalte im CSV). | Wer `ISeasonPasses` mockt/implementiert: neue `ImportAsync`-Methode. Wer `SeasonPass.Create` aufruft: `reference` ist optional (alte Aufrufe gültig). Kein Enum entfernt; nur additive nullable Spalte. |
| 06.07.2026 | S1 | **Kanonische Ticket-URL (in `main`):** neuer Service `IPublicBaseUrl.Resolve(request)` (Config `Tickets:PublicBaseUrl`, Azure gesetzt prod=`https://tickets.redants.ch` / dev=`https://tickets-dev.redants.ch`, Fallback = Request-Host lokal). Alle absoluten Ticket-URLs/QRs bauen jetzt darüber statt `Request.Host` (WebTicketController + `/qr.png`, Flex-/Saisonpass-/Mitglieder-Export, Checkout-Mail, Dev-Testmail) — im Backoffice auf `admin.redants.ch` erzeugte Links zeigten sonst auf den Admin-Host. | Wer neue Ticket-Links/QRs baut: `IPublicBaseUrl` injizieren statt `Request.Scheme://Request.Host`. Kein Schema/Enum. |
| 06.07.2026 | S1 | Runde-3-Plan verteilt (dieses Dokument). | Alle: Runde-3-Branch anlegen, Paket abarbeiten. |
| 05.07.2026 | S1 | Gate-Ausnahmen ergänzt (`Program.cs`): `/App_Plugins`, `/css`, `/js`, `/lib`, `scanner-sw.js`, `site.webmanifest` — sonst blockte das Cookie-Gate Backoffice-Dashboard-Element und statische Assets (leere/ungestylte Seiten). | Wer am Gate arbeitet: neue Assets/Backoffice-Pfade in `IsExempt` aufnehmen. |
| 05.07.2026 | S1 | `FreeEntryType.Child` (int 4, „Kind (gratis)"); getrennte Saison-Kategorie-Angebote `PassOffered`/`PassQuota` und `TicketOffered`/`TicketQuota` (`SeasonCategoryPrice`, additive Spalten `TicketOffered`/`TicketQuota`); neue Anlässe erben Saison-Ticketstandards (`EventPriceDefaults`); `TicketEventFreeEntryQuotas` (SU-Kontingent); Abendkasse-Express (`/heute`, `/kasse/express`, QR auf Bestätigung); Mail „Ticket X von Y". | Wer `SeasonCategoryPrice`/`IAdmissionService`/`FreeEntryType` nutzt: neue Signaturen/Werte. |
