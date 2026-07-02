# RedAnts

Public website and self-service ticketing application for Red Ants Winterthur, built on **Umbraco CMS 17 / .NET 10**.

## Requirements

- .NET 10 SDK
- No external database needed: the app uses SQLite, created on first run.

## Run locally

```bash
dotnet run
```

The dev profile serves on:

- `https://localhost:44370` (and `http://localhost:54363`)
- Backoffice: `/umbraco`

On first start the app installs unattended (see `appsettings.Development.json`) with:

- User: `admin@localhost.dev`
- Password: `Admin1234!`

The SQLite database is created at `umbraco/Data/Umbraco.sqlite.db` (WAL mode is enabled before boot). Content types and sample content are seeded in code on startup, so no manual backoffice setup is required.

Local development runs with test Turnstile keys and empty Payrexx credentials; payment and captcha are effectively stubbed until real secrets are supplied via user secrets or configuration.

## Project layout

| Path | Purpose |
|------|---------|
| `Domain/` | Pure domain models, enums, value objects (no framework deps). |
| `Features/` | Application layer by use case, with `Ports/` interfaces. |
| `Infrastructure/` | Adapters: Umbraco integration, repositories, payment, email. Split into `Shared`, `Ticketing`, `Website`. |
| `Views/` | Razor views for the public website and ticketing pages. |
| `wwwroot/` | Static assets (`css/site.css` etc.). |
| `uSync/` | uSync content-type / configuration snapshots. |

## Documentation

- `ARCHITECTURE.md`: design, layering, content-slice model, code-first seeding, and the runtime-compiled Razor caveats.
- `CLAUDE.md` / `AGENTS.md`: working conventions for AI coding agents.

## Tech stack

Umbraco 17, .NET 10, SQLite, uSync, Sqids (opaque URL ids), Payrexx (payment), Brevo (email), Cloudflare Turnstile (captcha). Default culture is Swiss German (`de-CH`).
