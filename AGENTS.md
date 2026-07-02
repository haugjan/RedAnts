# AGENTS.md

Guidance for AI coding agents working in this repository. This mirrors `CLAUDE.md`; both point to `ARCHITECTURE.md` for the full design. Read `ARCHITECTURE.md` before making structural changes.

## What this is

Umbraco 17 / .NET 10 app with a public website and a ticketing system, backed by SQLite. Run with `dotnet run` (see `README.md`).

## Non-negotiable rules

- **Aliases only from `*Aliases` classes.** Use `WebsiteAliases` / `TicketingAliases` for content type and property aliases. Never scatter alias string literals in code.
- **ModelsBuilder is off (`ModelsMode = Nothing`).** Read all content untyped via `.Value<T>(alias)`. Do not create or assume generated typed models.
- **Razor is runtime-compiled** (`RazorCompileOnBuild=false`). `dotnet build` does not catch `.cshtml` errors; validate views by running the app. Never name a `foreach` variable `page` (it collides with the `@page` directive and breaks compilation); use `item`.
- **Content types are code-first and idempotent.** Add or change them in the seeder (`*ContentTypeSeeder`) via check-then-create/reconcile, not by hand in the backoffice.
- **Keep the two slices decoupled.** Website code must not reach into ticketing internals; go through ports or the published-content API.
- **Secrets** (Payrexx, Brevo, Turnstile) come from configuration / user secrets. Never hardcode them.

## Common tasks

- **New website block element**: add the element type + property aliases to `WebsiteAliases`, create/reconcile it in `WebsiteContentTypeSeeder`, register the block in the "Website Content Blocks" Block List, add `Views/Partials/Blocks/{alias}.cshtml`, and add styles to `wwwroot/css/site.css`. If the block can lead a page and should float the nav, wire `ViewBag.TransparentNav`.
- **New ticketing behavior**: add/extend a port in `Features/Ticketing/Ports/`, implement the adapter in `Infrastructure/Ticketing/`, keep controllers depending only on ports.

## Verifying changes

- Build: `dotnet build` (catches C# errors, not Razor).
- For view or content-type changes, run `dotnet run` and hit the affected page / the backoffice at `/umbraco`.
