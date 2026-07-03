# Architecture

RedAnts is an Umbraco 17 / .NET 10 application combining a public marketing website with a self-service ticketing system, backed by SQLite.

## Layering (ports and adapters)

Three top-level layers, with dependencies pointing inward (`Infrastructure` → `Features` → `Domain`):

### `Domain/`
Pure business model: entities, enums, value objects, and `DomainException`. No dependency on Umbraco, ASP.NET, or any framework. Examples: `Domain/Ticketing/` (`Event`, `Season`, `Venue`, `BillingAddress`, `PostalCode`) and `Domain/Ticketing/Sales/` (`Order`, `EventTicket`, `SeasonSingleTicket`, `SeasonPass`, `MemberCard`, `EventVisit`/`EventVisitLog`/`EventFreeEntry`, the pricing aggregates `EventPrice`/`SeasonPrice`/`CategoryPrice`, and the enums in `SalesEnums.cs`).

### `Features/`
Application layer, organized by use case rather than by technical type:

- `Features/Ticketing/Ports/` defines the interfaces the application depends on: `CatalogPorts` (read-only Season/Venue/Event) and `SalesCatalogPorts` (pricing read/write `IEventPrices`/`ISeasonPrices`, the resolved read side `IEventPricing`, issued `IEventTickets`).
- Use-case folders hold controllers and Blazor components: `Public/` (browsing, access gate), `Cart/` (guest shopping cart), `Admin/` (backoffice ticketing section and its admission-count report models), `Scanning/` (QR scanner).

Controllers depend only on ports, never on concrete infrastructure.

### `Infrastructure/`
Adapters that implement the ports and integrate with Umbraco and external services. Organized by slice:

- `Infrastructure/Shared/`: cross-cutting helpers (`SqidEncoder`, `EmailLayout`).
- `Infrastructure/Ticketing/`: catalog readers over Umbraco content (`UmbracoCatalogReaders`), the `Sales/` slice (NPoco records + repositories for orders/tickets/pricing, `PricingCatalogSeeder`), the single-step schema `TicketingMigration`, `SessionCartService`, the `Admin/` report readers, and the `TicketingComposer` that wires it up.
- `Infrastructure/Website/`: the public website content types and seeding.

## Two content slices

Two independent slices share the Umbraco instance and must stay decoupled:

1. **Ticketing**: events, seasons, event tickets / season single tickets / season passes / member cards, a guest shopping cart, per-event and per-season pricing with quotas, admission scanning, and a purchase flow with Payrexx payment and Brevo confirmation email.
2. **Website**: the public marketing site. A `flexPage` document type holds a `content` Block List of element types (`heroElement`, `headerElement`, `eventListElement`), rendered through `Views/FlexPage.cshtml`, `Views/Shared/_SiteLayout.cshtml`, and `Views/Partials/Blocks/{alias}.cshtml`.

Website code must not reach into ticketing internals. If a real dependency arises, route it through a port. The event-list block is the sanctioned bridge: it reads ticketing seasons through the published-content API, not through ticketing internals.

## Ticketing data model

The catalog (Season, Venue, Event) is Umbraco Document Types. Everything transactional is NPoco tables, created in a **single migration step** (`CreateTicketingSchema` in `Infrastructure/Ticketing/TicketingMigration.cs`) with no historical/incremental steps: the app installs a fresh schema, so a pre-existing dev database must be deleted to recreate it cleanly. See `README.md` for the full entity-relationship diagram and enum value tables.

Design rules:

- **Enums are persisted as their integer value.** `Category` (`TicketCategory`), `Status` (`OrderStatus`/`TicketStatus`), `PaymentMethod`, `TicketType`, `FreeEntryType`, and the visit-log `Type` (`VisitLogType`) are all `int` columns. Records store `(int)theEnum`; repositories cast back, and raw SQL in the admin report readers compares against `(int)` casts. There is no category master table and no `CategoryCode`/`CategoryName` — the display label comes from `TicketCategory.DisplayName()`.
- **No enforced foreign keys.** Tables reference each other (and the Umbraco catalog) by id only; `EventId`/`SeasonId` hold the Umbraco content node id. This keeps the ticketing tables decoupled from the content tree and from each other.
- **Pricing (`EventPrices`/`SeasonPrices`) is 0..1 per event/season** (unique on the node id), each with n category sub-rows (`Category`, `SalePrice`, `Quota?`). `EventPrices` additionally carries the event-level `TotalSalesQuota` (Verkaufskontingent gesamt) and `AdmissionQuota` (Einlasskontingent, persons). `EventPricingReader` marks a category sold out once its `Quota` or the event's `TotalSalesQuota` is reached by the valid issued `EventTickets`.
- **Admissions are one `TicketEventVisits` row per `(event, ticket)`.** There is no `CheckedOutAt`; the individual in/out scans are appended to `TicketEventVisitsLogs`, and `IsInside` is the visit's current presence. Free-entry persons (players, staff, officials, Swiss Unihockey free cards) are admitted spontaneously with `TicketType = FreeEntry` (`TicketUuid` null) and their kind recorded in `TicketEventFreeEntries` (`FreeEntryType`).
- `EventTicket` and `SeasonSingleTicket` carry a `bool Redeemed` (the single-admission tickets); `SeasonPass` and `MemberCard` are multi-event and have no redemption flag. `Orders` is the immutable financial record; tickets link to it by optional `OrderId`.

`PricingCatalogSeeder` idempotently gives every event and season a base price set (Adult/Youth/Child) on boot and seeds a few demo `EventTickets`.

## Code-first content types

Content types are defined and maintained in code, not by hand in the backoffice. Each slice follows the same pattern:

1. A `IComposer` registers an `INotificationAsyncHandler<UmbracoApplicationStartedNotification>`.
2. On application start the handler ensures document types, element types, and data types (e.g. the Block List) exist, then seeds sample content.
3. Every step is **idempotent** (check-then-create / reconcile), so it runs safely on every boot and on an existing database.
4. All type and property aliases live in one `*Aliases` static class per slice (`TicketingAliases`, `WebsiteAliases`). Never scatter alias string literals through the code.

Reference implementations: `Infrastructure/Ticketing/Content/TicketingContentTypeSeeder.cs`, `Infrastructure/Website/WebsiteContentTypeSeeder.cs`.

### Block List value format (Umbraco v17)

Block List content values are JSON with `contentData` (array of `{contentTypeKey, key, values:[{alias,culture,segment,value}]}`), optional `settingsData`, an `expose` array (`{contentKey,culture,segment}`), and `layout["Umbraco.BlockList"]` (array of `{contentKey,contentUdi,settingsKey,settingsUdi}`). The website seeder builds this JSON directly when creating sample pages.

## Reading content (ModelsBuilder = Nothing)

`Umbraco.CMS.ModelsBuilder.ModelsMode` is set to **`Nothing`** in `appsettings.json`. There are no generated typed models. Read all content untyped:

```csharp
var title = page.Value<string>(WebsiteAliases.HeroTitle);
var media = page.Value<MediaWithCrops>(WebsiteAliases.HeroImage);
```

Do not introduce or assume ModelsBuilder-generated classes.

## Razor is runtime-compiled

`RazorCompileOnBuild` and `RazorCompileOnPublish` are `false` (needed for the backoffice to work). Therefore:

- `dotnet build` does not compile `.cshtml` files, so it will not surface view errors. Validate views by running the app and requesting the page.
- Known trap: naming a `foreach` variable `page` breaks compilation, because `@page.Url()` is parsed as the `@page` Razor directive. Use another name such as `item`.

## Routing and URLs

- Ticketing links use fixed MVC routes (for example `/tickets/event/{sqid}`, `/saisonkarten/{sqid}`), where `{sqid}` is a Sqids-encoded id. They do not depend on Umbraco content-node URLs, so reordering root nodes is safe.
- The public homepage is the first `flexPage` at the content root. The website seeder sorts it to the first root position so it serves at `/`, which demotes the ticketing content node's URL harmlessly (ticketing uses the fixed routes above).

## Adaptive navigation

`Views/Shared/_SiteLayout.cshtml` renders a shared nav. When a page opens with a hero or header block, `ViewBag.TransparentNav = true` makes the fixed bar float transparent over the image with white text; a scroll handler switches it to a solid white background with black text past 60px. Pages without a leading hero/header get a solid nav from the start.

## Infrastructure and boot

- **Database**: Development uses **SQLite** (`appsettings.Development.json`); `Program.cs` resolves the SQLite path to absolute and enables WAL mode before Umbraco boots, preventing `SQLITE_BUSY` when the migrator, OpenIddict/EF Core, and NPoco open the file concurrently. That bootstrap is guarded by the provider name, so it is skipped in production. Production uses **Azure SQL** (`appsettings.json` sets provider `Microsoft.Data.SqlClient`, empty DSN; the real connection string is injected as the App Service app setting `ConnectionStrings__umbracoDbDSN`). The SQL Server provider ships transitively with `Umbraco.Cms`.
- **Deployment**: GitHub Actions (`.github/workflows/deploy.yml`) build/publish/ZipDeploy to an Azure App Service in resource group `RG_RedAnts` on push to `main`. One-time provisioning script and details in `deploy/README.md` and `deploy/azure-setup.sh`.
- **Culture**: default thread culture is `de-CH` (Swiss German date/number formatting), set at the top of `Program.cs`.
- **Development**: OpenIddict transport security is disabled in Development so the backoffice works over HTTP; unattended install/upgrade is enabled.
- **External services**: Payrexx, Brevo, and Turnstile are configured via `appsettings` / user secrets. Secrets are never hardcoded; Development uses Cloudflare test Turnstile keys and empty Payrexx credentials.
