# RedAnts

Public website plus a self-service ticketing application for Red Ants Winterthur, built on **Umbraco CMS 17 / .NET 10** with Azure SQL storage.

## Tech stack

- Umbraco.Cms 17 on `net10.0`, `ImplicitUsings` and `Nullable` enabled.
- Persistence: **Azure SQL in every environment** (provider `Microsoft.Data.SqlClient`, ships transitively with Umbraco). `appsettings.json` carries an empty DSN; the real connection string comes from the App Service app setting `ConnectionStrings__umbracoDbDSN`, written by the pipeline from the GitHub environment variable `APP_DSN`. **No SQL passwords**: the App Service authenticates with its managed identity (`Authentication=Active Directory Managed Identity`), the pipeline and local runs with `Active Directory Default` (az CLI login). Blob Storage (media, Show sounds) is reached the same way via `…:AccountUrl` settings and `DefaultAzureCredential`; `docs/setup-entra-access.ps1` creates the database users and role assignments. No SQLite bootstrap: the earlier SQLite + WAL setup was removed when dev and prod were unified on Azure SQL.
- uSync for content-type/config sync (`uSync/v17`).
- Sqids for opaque public URL identifiers.
- Default culture is Swiss German (`de-CH`), set globally in `Program.cs` (dd.MM.yyyy dates, apostrophe thousands separator).

## Architecture

The solution (`RedAnts.slnx`) is split into three projects plus one test project per slice:

- **`src/RedAnts.Host`** (web app, `AssemblyName=RedAnts`) — `Program.cs` (host-based routing, health, dev badge, CSP, 404, site gate), Umbraco boot, `Infrastructure/Shared/` (Entra backoffice auth, themes), the Website slice (`Infrastructure/Website/`, `Features/Website/`), `uSync/`, the Umbraco template views under `Views/` and the shared wwwroot assets (site.css, favicons, PWA manifest, scanner service worker).
- **`src/RedAnts.Ticketing`** (Razor class library, compiled views) — the whole ticketing slice: `Domain/`, `Features/Ticketing/` (ports, controllers, Blazor components), `Infrastructure/Ticketing/` (NPoco repos, migrations, email outbox, Payrexx, PDF/QR), the plain MVC views, the `/scan` Razor Page and the ticketing css/js served under `/_content/RedAnts.Ticketing/`. The Host consumes it via `AddTicketing(...)`, `UseTicketingShortHostRedirect()`, `UseTicketingAnalytics()` and `UseTicketingScanAuth()`.
- **`src/RedAnts.Show`** (Razor class library, placeholder) — future soundboard/light control at `show[-dev].redants.ch` → `/show`, backoffice section "Show" (iframe to `/admin/show`), own SQL schema `show` created idempotently on `ConnectionStrings:showDbDSN` (fallback: Umbraco DB), prepared `Show:Storage` blob options.

Each project layers internally as Domain → Features (ports) → Infrastructure (adapters). Umbraco composers in the class libraries are discovered by Umbraco's assembly scan; no extra wiring in `Program.cs` is needed.

The slices must stay decoupled:

1. **Ticketing** (`src/RedAnts.Ticketing`): events, seasons, event/season tickets, season passes, member cards, ticket bundles, season add-ons, a guest cart, per-season price tiers and per-event/season pricing with quotas, admission scanning, PDF/QR ticket delivery, Payrexx payment, Microsoft Graph email (drained from a SQL outbox), Turnstile captcha.
2. **Website** (`src/RedAnts.Host`: `Infrastructure/Website/`, `Views/`): FlexPage + block elements, legal pages, robots/sitemap.
3. **Show** (`src/RedAnts.Show`): placeholder, own schema and storage config, no Umbraco content.

Exception forced by Umbraco: the ticketing **template** views (`TicketingHome`, `TicketEvent`, `TicketSeason`, `TicketVenue`, `SaisonsPromo`) live in the Host's `Views/` folder because Umbraco templates are DB entities backed by physical content-root files (the seeder reads them from disk, the backoffice edits them). They may consume ticketing ports via `@inject`; Host *code* uses ticketing only through the extension methods above.

## Ticketing data model

Catalog entities (Season/Venue/Event) are Umbraco Document Types; sales, admissions, and pricing are NPoco tables built by `CreateTicketingSchema` (`Infrastructure/Ticketing/TicketingMigration.cs`). Each step is idempotent (each table is created only when missing and additive columns are gated by `ColumnExists`). **Migrations run as a pipeline step, not in the web app**: `dotnet RedAnts.dll --migrate` boots Umbraco, executes the Ticketing and Show plans and exits, using the GitHub OIDC identity which holds `db_ddladmin`. `Migrations:RunAtBoot` defaults to `true` (a new step runs on the next local `dotnet run`); the pipeline writes `Migrations__RunAtBoot=false` together with the passwordless `APP_DSN`, so an environment that has moved to the managed identity (no DDL) never migrates at boot, while an environment still on a SQL login keeps the old behaviour until it is switched. Umbraco skips a plan whose stored state already equals the final step; set `Migrations:ForceToken` to a new value to replay it once. Add new steps to the end of `TicketingMigrationPlan` with a unique state name. Full ER diagram + enum value tables live in `README.md`. Rules to follow when touching it:

- **Persist enums as their integer value** (`Category`, `Status`, `PaymentMethod`, `TicketType`, `FreeEntryType`, visit-log `Type` are `int` columns). No `CategoryCode`/`CategoryName` in the DB; labels come from `TicketCategory.DisplayName()`.
- **No FK constraints**; reference by id. `EventId`/`SeasonId` hold the Umbraco content node id.
- Pricing: `EventPrices`/`SeasonPrices` are 0..1 per node with n category rows (`Category`, `SalePrice`, `Quota?`); `EventPrices` also holds `TotalSalesQuota` + `AdmissionQuota`.
- Admissions: one `TicketEventVisits` row per `(event, ticket)` (no `CheckedOutAt`); in/out scans go to `TicketEventVisitsLogs`; free-entry persons use `TicketType = FreeEntry` + `TicketEventFreeEntries`.
- NPoco async calls (`FetchAsync`/`ExecuteAsync`) need `using NPoco;`.

## Content types are code-first

Content types are **not** managed by hand in the backoffice. Each slice seeds them on startup:

- A `IComposer` registers an `INotificationAsyncHandler<UmbracoApplicationStartedNotification>`.
- The handler creates document/element types, data types (e.g. Block List), and sample content **idempotently** (check-then-create, safe to run on every boot).
- Property and type aliases live in a single `*Aliases` static class per slice (`TicketingAliases`, `WebsiteAliases`). Reference aliases from there, never as string literals scattered across the code.

Reference implementations: `Infrastructure/Ticketing/Content/TicketingContentTypeSeeder.cs` and `Infrastructure/Website/WebsiteContentTypeSeeder.cs`.

## Reading content values (important)

`ModelsBuilder.ModelsMode` is set to **`Nothing`** (see `appsettings.json`). There are **no generated strongly-typed models**. Read all content untyped:

```csharp
page.Value<string>(WebsiteAliases.HeroTitle);
page.Value<IPublishedContent>(alias);
```

Do not assume or generate ModelsBuilder classes.

## Razor compilation is split

- **Host views** (`src/RedAnts.Host/Views/`, the Umbraco templates and website views) are **runtime-compiled** (`RazorCompileOnBuild=false`, required for the backoffice). `dotnet build` does **not** catch errors there; run the app and hit the page. Gotcha: a `foreach` loop variable named `page` breaks compilation, because `@page.Url()` is parsed as the `@page` directive. Use a different name (e.g. `item`).
- **Ticketing and Show views** (in the class libraries) are **compiled at build time** — view errors surface in `dotnet build`. Their static assets are served under `/_content/<ProjectName>/…`; the site gate exempts `/_content`.

## Environments (custom domains)

Host-based routing (`Program.cs`): the root `/` redirects by host — `scan[-dev].redants.ch` → `/scan`, `admin[-dev].redants.ch` → `/umbraco`, `show[-dev].redants.ch` → `/show`, everything else → `/ticketing/`.

| Surface | PROD | DEV |
|---|---|---|
| Public / tickets | `tickets.redants.ch` | `tickets-dev.redants.ch` |
| Scanning | `scan.redants.ch` | `scan-dev.redants.ch` |
| Admin / backoffice | `admin.redants.ch` | `admin-dev.redants.ch` |
| Show (placeholder) | `show.redants.ch` | `show-dev.redants.ch` |

Underlying App Services: `app-redants-prod` / `app-redants-dev`. **Only `tickets.redants.ch` is search-indexed** (`index,follow`); every other host — all `*-dev`, plus `scan.*`/`admin.*` and `*.azurewebsites.net` — is `noindex,nofollow`.

## Public URLs

Ticketing public and intern links use **fixed MVC routes** (`/tickets/event/{sqid}`), not Umbraco content-node URLs. Reordering or demoting root content nodes therefore does not break ticketing links. All routes are English: public `/cart`, `/checkout` (+ `/express`, `/success`, `/cancel`, `/status`, `/confirmation`), `/seasons`, `/next` (+ `/embed` for the cross-site widget), `/scan`, `/ticket/{token}` (+ `/pdf`), plus `/payrexx/webhook`; admin `/admin/ticketing` (the Blazor dashboard) plus the CSV routes `/admin/members`, `/admin/season-passes`, `/admin/event-tickets`, `/admin/flex-tickets` (the earlier German routes were removed, no redirects). The website homepage is the first `flexPage` root node (the seeder sorts it to first so it serves at `/`).

## Conventions

- **No comments in code.** The code speaks for itself: prefer clear names and small well-named methods over explanatory comments. This covers line, block, XML-doc (`///`), Razor (`@* *@`), and embedded CSS/JS comments. Non-obvious "why" (design decisions, Swiss compliance, gotchas) goes in `ARCHITECTURE.md` under "Design rationale and gotchas", not inline.
- Keep the slices decoupled: no direct references from Website or Show code into Ticketing internals (go through ports if a genuine dependency arises). Cross-project code sharing beyond that needs a deliberate decision, not an ad-hoc reference.
- Tests are cut per slice: `tests/RedAnts.Host.Tests`, `tests/RedAnts.Ticketing.Tests`, `tests/RedAnts.Show.Tests`, each referencing its src project. `dotnet test RedAnts.slnx` runs in CI before publish.
- New website block elements: element type + alias in `WebsiteAliases`, register the block in the "Website Content Blocks" Block List, add a partial under `src/RedAnts.Host/Views/Partials/Blocks/{alias}.cshtml`, add styles to `src/RedAnts.Host/wwwroot/css/site.css`.
- Secrets (Payrexx, Microsoft Graph, Turnstile) come from configuration / user secrets, never hardcoded. User secrets live on the Host project (`--project src/RedAnts.Host`).

## Session workflow: preview & deploy

Parallel sessions (S1–S7) each work in their own worktree `C:\development\RedAnts-s<N>` on their own branch `feature/s<N>-<short>`, never directly on `main`, and commit immediately. After each change, classify it:

- **Simple (no DB/schema change)** — CSS, views/layout, text, front-end, PDF/mail templates, config without a migration: **run it locally, do NOT deploy to dev.** `dotnet run --project src/RedAnts.Host` (`ASPNETCORE_ENVIRONMENT=Development`, `--no-build` once built) on the session's own port `560<N>` (S1 → 5601 … S7 → 5607) against the Azure **dev** DB (the user-secrets DSN). Give the user the **localhost URL** (`http://localhost:560<N>/…`, reachable because the agent runs on the user's own machine) plus a one-line summary. Deploying every simple change to the single shared `app-redants-dev` makes parallel sessions overwrite each other, so don't.
- **Complicated** — DB schema/migrations/seeders, or a flow that needs the real domain (Payrexx payment, backoffice/OIDC login, host-based `scan.`/`admin.` behaviour): **push the feature branch** (the pipeline deploys DEV only; `deploy-prod` is gated to `main`), watch the run, and report the matching dev link — tickets `tickets-dev.redants.ch`, scanning `scan-dev.redants.ch`, admin `admin-dev.redants.ch` (prod: the same hosts without `-dev`).

Then always ask **"Auf prod deployen? Ja/Nein"** (Ja first, so the user can arrow + Enter). On **Ja**: `git push origin HEAD:main` (prod deploys); watch the CI build and report when green.

## Agent test track (verify without the user)

- **Azure access is session-isolated**: set `AZURE_CONFIG_DIR` to a folder in the session scratchpad and run `az login --use-device-code --tenant 64a8811c-a541-4b97-9571-5a8d280bd40b` there (the user completes it with the `@redants.ch` member account). Every `az` call, `docs/setup-entra-access.ps1` and the local app run with that variable; never touch the user's global az context.
- **Local run against the agent copy**: `dotnet run --project src/RedAnts.Host --launch-profile Agent --no-build` (port 5606) uses `sqldb-redants-agent` (a Basic copy of dev, re-copy with `az sql db copy` when it should be fresh), `Active Directory Default`, the classic backoffice login (`BackOfficeAuth` empty), `Email:Transports=None` (mail stays in `OutboxEmails`) and blob account URLs on `stredantsdev`.
- **Browser tests**: `tests/RedAnts.BrowserTests` (Playwright for .NET, Chromium via `bin/.../playwright.ps1 install chromium`). They skip unless `E2E_BASE_URL` is set: `E2E_BASE_URL=http://localhost:5606 dotnet test tests/RedAnts.BrowserTests`. Screenshots land in `E2E_SCREENSHOT_DIR` (default `bin/.../screenshots`); read them to verify visually. The backoffice tests log in as `agent@redants.ch` with `Agent:BackofficePassword` from the Host user secrets (the user sets it, it never appears in chat or repo).
