# RedAnts

Public website plus a self-service ticketing application for Red Ants Winterthur, built on **Umbraco CMS 17 / .NET 10** with Azure SQL storage.

## Tech stack

- Umbraco.Cms 17 on `net10.0`, `ImplicitUsings` and `Nullable` enabled.
- Persistence: **Azure SQL in every environment** (provider `Microsoft.Data.SqlClient`, ships transitively with Umbraco). `appsettings.json` carries an empty DSN; the real connection string comes from the App Service app setting `ConnectionStrings__umbracoDbDSN` (prod) or user secrets pointing at the shared dev database (local). No SQLite bootstrap: the earlier SQLite + WAL setup was removed when dev and prod were unified on Azure SQL.
- uSync for content-type/config sync (`uSync/v17`).
- Sqids for opaque public URL identifiers.
- Default culture is Swiss German (`de-CH`), set globally in `Program.cs` (dd.MM.yyyy dates, apostrophe thousands separator).

## Architecture

Layered / ports-and-adapters, split into three top-level folders:

- **`Domain/`** — pure domain models, enums, value objects, and `DomainException`. No framework or Umbraco dependencies. `Domain/Ticketing/` (catalog: `Event`, `Season`, `Venue`) and `Domain/Ticketing/Sales/` (`Order`, the ticket types, visits, pricing aggregates, enums).
- **`Features/`** — application layer, organized by use case. `Features/Ticketing/Ports/` holds the interfaces (`CatalogPorts`, `SalesCatalogPorts`); use-case folders (`Public/`, `Cart/`, `Admin/`, `Scanning/`) hold controllers and Blazor components.
- **`Infrastructure/`** — adapters that implement the ports and integrate with Umbraco and external services. Split by slice: `Shared/`, `Ticketing/` (with a `Sales/` sub-slice for the NPoco records/repositories), `Website/`.

Two independent content slices coexist and must stay decoupled:

1. **Ticketing** (`Infrastructure/Ticketing/`, `Features/Ticketing/`): events, seasons, event/season tickets, season passes, member cards, a guest cart, per-event/season pricing with quotas, admission scanning, Payrexx payment, Brevo email, Turnstile captcha.
2. **Website** (`Infrastructure/Website/`, `Views/`): the public marketing site (FlexPage + block elements, adaptive navigation).

## Ticketing data model

Catalog entities (Season/Venue/Event) are Umbraco Document Types; sales, admissions, and pricing are NPoco tables built by `CreateTicketingSchema` (`Infrastructure/Ticketing/TicketingMigration.cs`). The build is idempotent and re-runs on every boot (the recorded migration state is reset first; each table is created only when missing and additive columns are gated by `ColumnExists`), so a new table or column appears on the next start with no database drop. Full ER diagram + enum value tables live in `README.md`. Rules to follow when touching it:

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

## Razor is runtime-compiled

`RazorCompileOnBuild` and `RazorCompileOnPublish` are **`false`** (required for the backoffice). Consequences:

- `dotnet build` does **not** catch errors in `.cshtml` files. To validate a view you must run the app and hit the page.
- Gotcha: a `foreach` loop variable named `page` breaks compilation, because `@page.Url()` is parsed as the `@page` directive. Use a different name (e.g. `item`).

## Public URLs

Ticketing public and intern links use **fixed MVC routes** (`/tickets/event/{sqid}`, `/saisonkarten/{sqid}`), not Umbraco content-node URLs. Reordering or demoting root content nodes therefore does not break ticketing links. The website homepage is the first `flexPage` root node (the seeder sorts it to first so it serves at `/`).

## Conventions

- **No comments in code.** The code speaks for itself: prefer clear names and small well-named methods over explanatory comments. This covers line, block, XML-doc (`///`), Razor (`@* *@`), and embedded CSS/JS comments. Non-obvious "why" (design decisions, Swiss compliance, gotchas) goes in `ARCHITECTURE.md` under "Design rationale and gotchas", not inline.
- Keep the two slices decoupled: no direct references from Website code into Ticketing internals (go through ports if a genuine dependency arises).
- New website block elements: element type + alias in `WebsiteAliases`, register the block in the "Website Content Blocks" Block List, add a partial under `Views/Partials/Blocks/{alias}.cshtml`, add styles to `wwwroot/css/site.css`.
- Secrets (Payrexx, Brevo, Turnstile) come from configuration / user secrets, never hardcoded.
