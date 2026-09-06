# AGENTS.md

Guidance for AI coding agents working in this repository. This mirrors `CLAUDE.md`; both point to `ARCHITECTURE.md` for the full design. Read `ARCHITECTURE.md` before making structural changes.

## What this is

Umbraco 17 / .NET 10 app with a public website and a ticketing system, backed by **Azure SQL in every environment** (local dev points at the shared dev database via user secrets; SQLite has been removed). The solution `RedAnts-WebApp/RedAnts.slnx` is split per slice: `RedAnts-WebApp/src/RedAnts.Host` (web app, Umbraco, Website slice, template views), `RedAnts-WebApp/src/RedAnts.Ticketing` (Razor class library, whole ticketing slice, compiled views), `RedAnts-WebApp/src/RedAnts.Show` (placeholder for the soundboard/light-control app), plus one test project per slice under `tests/`. Run with `dotnet run --project RedAnts-WebApp/src/RedAnts.Host` (see `README.md`).

## Non-negotiable rules

- **Aliases only from `*Aliases` classes.** Use `WebsiteAliases` / `TicketingAliases` for content type and property aliases. Never scatter alias string literals in code.
- **ModelsBuilder is off (`ModelsMode = Nothing`).** Read all content untyped via `.Value<T>(alias)`. Do not create or assume generated typed models.
- **Razor compilation is split.** Host views (`RedAnts-WebApp/src/RedAnts.Host/Views/`, the Umbraco templates) are runtime-compiled: `dotnet build` does not catch their errors; validate by running the app, and never name a `foreach` variable `page` (it collides with the `@page` directive; use `item`). Ticketing/Show views compile at build time; their static assets are served under `/_content/<ProjectName>/`.
- **Content types are code-first and idempotent.** Add or change them in the seeder (`*ContentTypeSeeder`) via check-then-create/reconcile, not by hand in the backoffice.
- **Keep the slices decoupled.** Website and Show code must not reach into ticketing internals; go through ports or the published-content API. The Host consumes ticketing only via the `AddTicketing(...)` / `UseTicketing*()` extension methods.
- **Secrets** (Payrexx, Microsoft Graph, Turnstile) come from configuration / user secrets. Never hardcode them.

## Common tasks

- **New website block element**: add the element type + property aliases to `WebsiteAliases`, create/reconcile it in `WebsiteContentTypeSeeder`, register the block in the "Website Content Blocks" Block List, add `RedAnts-WebApp/src/RedAnts.Host/Views/Partials/Blocks/{alias}.cshtml`, and add styles to `RedAnts-WebApp/src/RedAnts.Host/wwwroot/css/site.css`. If the block can lead a page and should float the nav, wire `ViewBag.TransparentNav`.
- **New ticketing behavior**: add/extend a port in `RedAnts-WebApp/src/RedAnts.Ticketing/Features/Ticketing/Ports/`, implement the adapter in `RedAnts-WebApp/src/RedAnts.Ticketing/Infrastructure/Ticketing/`, keep controllers depending only on ports.

## Verifying changes

- Build: `dotnet build RedAnts-WebApp/RedAnts.slnx` (catches C# errors and Ticketing/Show view errors, but not Host view errors).
- Tests: `dotnet test RedAnts-WebApp/RedAnts.slnx` (also runs in CI before publish).
- For Host view or content-type changes, run `dotnet run --project RedAnts-WebApp/src/RedAnts.Host` and hit the affected page / the backoffice at `/umbraco`.
