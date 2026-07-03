# Session-Koordination (parallele Claude-Code-Sessions)

Diese Datei koordiniert **mehrere gleichzeitig laufende Claude-Code-Sessions** im selben Repo.
Sie ist kein Ersatz für Git, sondern verhindert, dass zwei Sessions dieselben Dateien
gegenseitig überschreiben oder dasselbe Datenmodell in unterschiedliche Richtungen ändern.

> Es gibt **keine** Live-Verbindung zwischen den Sessions. Synchronisation passiert nur über
> (a) diese Datei, (b) häufige kleine Git-Commits. Jede Session liest diese Datei zu Beginn
> jeder Aufgabe und trägt ihren Anspruch ein, **bevor** sie schreibt.

## Protokoll (jede Session hält sich daran)

1. **Vor dem Editieren**: diese Datei lesen. Prüfen, ob eine andere Session die Datei/den
   Bereich beansprucht (Tabelle unten). Falls ja: nicht anfassen, sondern melden.
2. **Claim eintragen**: In der Tabelle „Aktive Ansprüche" eine Zeile mit Session-Kennung,
   Bereich, konkreten Dateien und Kurzstatus setzen, dann committen.
3. **Gemeinsame Dateien** (Liste unten) sind Hotspots: **immer** direkt vor dem Bearbeiten neu
   einlesen (`git pull --rebase` bzw. Datei frisch lesen). Nur additiv ändern wenn möglich,
   keine fremden Abschnitte umschreiben.
4. **Kleine, häufige Commits**. Nur die eigenen Dateien stagen (`git add <datei>`), **nie**
   `git add .` / `git add -A` (zieht fremde WIP-Änderungen mit). Commit-Message ohne
   Co-Authored-By-Zeile.
5. **Nach Fertigstellung**: Anspruch aus „Aktive Ansprüche" entfernen (oder auf „erledigt"
   setzen) und in „Änderungs-Log" eine Zeile ergänzen.
6. **Nie committen/pushen ohne ausdrückliche Aufforderung des Nutzers.**

## Aktive Ansprüche

| Session | Bereich | Dateien (Eigentum) | Status |
|---|---|---|---|
| S1 | Admin: Anlässe- + Mitgliederkarten-Tabellen (Backoffice-Blazor) | `Features/Ticketing/Admin/AdminEventsComponent.razor`, `AdminMemberCardsComponent.razor`, `MemberCardAdminReport.cs`, `EventAdmissionReport.cs`, `Infrastructure/Ticketing/Admin/MemberCardAdminReportReader.cs`, `EventAdmissionReportReader.cs` | in Arbeit |
| S3 | Azure-Deploy + Umbraco-Media auf Azure Blob Storage | `Program.cs` (nur additive Zeile: Blob-Media-Provider), `Directory.Packages.props`, `RedAnts.csproj`, `deploy/azure-setup.sh`, `deploy/README.md`, `.github/workflows/deploy.yml` | in Arbeit |
| S2 | Admin-Ticketverwaltung + Namensvereinheitlichung Tickettypen (Backoffice-Blazor) | `Features/Ticketing/Admin/AdminTicketsComponent.razor`, `Features/Ticketing/Admin/TicketTypeDisplay.cs` (NEU), `Features/Ticketing/Admin/TicketingAdminComponent.razor` (Tab-Labels) | erledigt/kompiliert |
| S5 | QR-Ticketzustellung (signierter Token, Scan-Endpoint, Web-Ticket mit Live-QR, Mail-QR, Google Wallet) | `Features/Ticketing/Scanning/*`, `Features/Ticketing/Public/*` (Web-Ticket), `Infrastructure/Shared/EmailLayout.cs` + Mailversand, neue Token-/QR-Helfer; ausserdem `Views/Shared/_SiteLayout.cshtml` (Warenkorb-Razor-Fix, uncommittet) | in Arbeit |
| S5 | Beispieldaten Saison-/Mitgliederkarten (idempotenter Boot-Seeder) | `Infrastructure/Ticketing/Seeding/SampleCardsSeeder.cs` (NEU, eigener Ordner ausserhalb S4-`Sales/*`); referenziert nur lesend S4-Typen + Enums, editiert keine fremde Datei | in Arbeit |
| S4 | Ticketing Sales/Pricing-Datenmodell + Doku | `Domain/Ticketing/Sales/*`, `Infrastructure/Ticketing/Sales/*`, `Infrastructure/Ticketing/TicketingMigration.cs`, `Features/Ticketing/Ports/SalesCatalogPorts.cs`, `Features/Ticketing/Cart/*`, `Infrastructure/Ticketing/SessionCartService.cs`, `Views/TicketEvent.cshtml`, `README.md`, `CLAUDE.md`, `ARCHITECTURE.md` | erledigt (Single-Step-Schema, Enums als int, Doku aktuell) |
| S4 | Admin: gemerkte Saison über Tabs + Reload beim Tab-Wechsel (Foundation) | `Features/Ticketing/Admin/TicketingAdminState.cs` (NEU), `Features/Ticketing/Admin/TicketingAdminManifest.cs` (nur additive DI-Zeile), `Features/Ticketing/Admin/AdminSeasonCardsComponent.razor` (Saisonkarten-Tab verdrahtet) | Foundation erledigt; S1/S2-Tabs ziehen nach (siehe Änderungs-Log) |

## Gemeinsame Dateien (Hotspots — vor dem Editieren immer neu einlesen)

Diese Dateien werden von mehreren Bereichen berührt. Änderungen hier bitte ankündigen und
möglichst additiv halten.

- `Domain/Ticketing/Sales/SalesEnums.cs` — Enums (TicketType, TicketCategory, TicketStatus, …)
- `Infrastructure/Ticketing/Sales/SalesRecords.cs` — NPoco-Records (Tabellen-Schema)
- `Infrastructure/Ticketing/TicketingMigration.cs` — Schema-Erstellung (Single-Step)
- `Features/Ticketing/Ports/CatalogPorts.cs`, `SalesCatalogPorts.cs` — Ports
- `Infrastructure/Ticketing/TicketingAliases.cs` — Content-Type-/Property-Aliases
- `Infrastructure/Ticketing/Content/TicketingContentTypeSeeder.cs` — Content-Type-Seeding
- `Program.cs`, `appsettings.json` — App-Bootstrap/Konfiguration
- `README.md`, `CLAUDE.md` — Doku/Projektregeln

**Vereinbarung zum Datenmodell**: Enums werden als **Integer** persistiert; Records verwenden
`int`-Spalten (`Category`, `Status`, `TicketType`, …), Labels über `DisplayName()`-Extensions.
Wer das Schema ändert, trägt es sofort ins „Änderungs-Log" ein, damit die anderen Sessions ihre
Reader/Records anpassen.

## Änderungs-Log (Schema/Contracts, die andere betreffen)

Nur Änderungen eintragen, die **andere Sessions** betreffen (Schema, Enums, Port-Signaturen,
gemeinsame DI-Registrierungen). Neueste zuerst.

| Datum | Session | Was geändert wurde | Auswirkung auf andere |
|---|---|---|---|
| _(TT.MM.JJJJ)_ | _S?_ | _z. B. „MembershipCards.Status von string auf int"_ | _z. B. „Reader müssen (TicketStatus)-Cast nutzen"_ |
| 03.07.2026 | S3 | Neues Paket `Umbraco.StorageProviders.AzureBlob` (17.1.0); in `Program.cs` additiv `AddAzureBlobMediaFileSystem()` in der Umbraco-Builder-Kette. Blob-Verbindung/Container nur als Azure-App-Settings (`Umbraco__Storage__AzureBlob__Media__*`), nicht in `appsettings.json`. | Keine Schema-/Enum-/Port-Änderung. Wer `Program.cs` anfasst: eine zusätzliche `.AddAzureBlobMediaFileSystem()`-Zeile in der Builder-Kette beachten. |
| 03.07.2026 | S2 | Kanonische deutsche Labels für `TicketType` als neue Extension `TicketTypeExtensions.DisplayName()` in `Features/Ticketing/Admin/TicketTypeDisplay.cs` (NEU, bewusst nicht in S4s `SalesEnums.cs`): EventTicket=Spieltickets, SeasonSingle=Flextickets, SeasonPass=Saisonkarten, MemberCard=Mitglieder, FreeEntry=Berechtigte (EventTicket/SeasonSingle nach Rückfrage von „Einzeleintritte/Einzelspiele" auf „Spieltickets/Flextickets" geschärft: fest an ein Spiel vs. beliebiges Spiel der Saison). Tab-Labels in `TicketingAdminComponent.razor` angepasst: „Tickets"→„Spieltickets", „Mitgliederkarten"→„Mitglieder". | Kein Schema/Enum/Port geändert. **S4**: falls ihr `TicketType.DisplayName()` selbst in `SalesEnums.cs` ergänzt, würde es mit meiner Extension kollidieren (mehrdeutiger Aufruf) — bitte stattdessen meine nutzen oder mit mir abstimmen. **S1**: der Tab heißt jetzt „Mitglieder" statt „Mitgliederkarten" (nur Label in der Nav, eure Komponenten unberührt). |
| 03.07.2026 | S4 | Scoped Blazor-Service `TicketingAdminState { int? SelectedSeasonId }` (`Features/Ticketing/Admin/TicketingAdminState.cs`) und DI-Registrierung in `TicketingAdminComposer` liegen bereits committet in HEAD (via `7a817cd`); ich habe ihn genutzt und den **Saisonkarten-Tab** (`AdminSeasonCardsComponent.razor`) verdrahtet, sodass die gewählte Saison tab-übergreifend gemerkt wird (scoped = pro SignalR-Circuit). Reload beim Tab-Wechsel passiert bereits, weil der Container pro Tab einen anderen Komponententyp rendert (Neuerzeugung → `OnInitializedAsync`). | **S1** (`AdminEventsComponent`, `AdminMemberCardsComponent`) und **S2** (`AdminTicketsComponent`): bitte Muster übernehmen, damit die Saison auch auf euren Tabs gemerkt wird — `[Inject] TicketingAdminState AdminState;`, in `OnInitializedAsync` `SelectedSeasonId = AdminState.SelectedSeasonId is int id && _seasons.Any(s => s.Id == id) ? id : <aktuelle Saison>;`, und im Reload/`@bind:after` `AdminState.SelectedSeasonId = SelectedSeasonId > 0 ? SelectedSeasonId : null;` zurückschreiben. Container `TicketingAdminComponent.razor` (S2) muss dafür NICHT geändert werden. |
