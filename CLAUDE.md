# RedAnts

Public website plus a self-service ticketing application for Red Ants Winterthur, built on **Umbraco CMS 17 / .NET 10** with SQLite storage.

## Tech stack

- Umbraco.Cms 17 on `net10.0`, `ImplicitUsings` and `Nullable` enabled.
- Persistence: SQLite (`Umbraco.Cms.Persistence.Sqlite`). WAL mode is pre-enabled in `Program.cs` before Umbraco boots to avoid `SQLITE_BUSY` during migration.
- uSync for content-type/config sync (`uSync/v17`).
- Sqids for opaque public URL identifiers.
- Default culture is Swiss German (`de-CH`), set globally in `Program.cs` (dd.MM.yyyy dates, apostrophe thousands separator).

## Architecture

Layered / ports-and-adapters, split into three top-level folders:

- **`Domain/`** — pure domain models, enums, value objects, and `DomainException`. No framework or Umbraco dependencies. Currently `Domain/Ticketing/`.
- **`Features/`** — application layer, organized by use case. `Features/Ticketing/Ports/` holds the interfaces (`CatalogPorts`, `TicketPorts`, `CrossCuttingPorts`); use-case folders (`Public/`, `Purchase/`) hold controllers and request/response models.
- **`Infrastructure/`** — adapters that implement the ports and integrate with Umbraco and external services. Split by slice: `Shared/`, `Ticketing/`, `Website/`.

Two independent content slices coexist and must stay decoupled:

1. **Ticketing** (`Infrastructure/Ticketing/`, `Features/Ticketing/`): events, seasons, single and season tickets, Payrexx payment, Brevo email, Turnstile captcha.
2. **Website** (`Infrastructure/Website/`, `Views/`): the public marketing site (FlexPage + block elements, adaptive navigation).

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

- Keep the two slices decoupled: no direct references from Website code into Ticketing internals (go through ports if a genuine dependency arises).
- New website block elements: element type + alias in `WebsiteAliases`, register the block in the "Website Content Blocks" Block List, add a partial under `Views/Partials/Blocks/{alias}.cshtml`, add styles to `wwwroot/css/site.css`.
- Secrets (Payrexx, Brevo, Turnstile) come from configuration / user secrets, never hardcoded.
