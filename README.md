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

## Data model (ticketing)

Catalog entities (Season, Venue, Event) are **Umbraco Document Types**, not database tables. Sales, admissions, and pricing live in NPoco tables created in one step by `CreateTicketingSchema` (`Infrastructure/Ticketing/TicketingMigration.cs`). The schema is a fresh install: to recreate it, delete the dev SQLite file (`umbraco/Data/Umbraco.sqlite.db`) and restart.

Conventions:

- **Enums are stored as their integer value** (not `nvarchar`): `Category`, `Status`, `PaymentMethod`, `TicketType`, `FreeEntryType`, and the visit-log `Type` columns are `int`.
- **No enforced foreign-key constraints** (loose coupling; relationships below are logical). `EventId` / `SeasonId` hold the **Umbraco content node id** of the event/season.
- A ticket's admission is one `TicketEventVisits` row per `(event, ticket)`; the individual in/out scans are appended to `TicketEventVisitsLogs`. `TicketEventVisits.TicketUuid` is a polymorphic link to the `Uuid` of the ticket named by `TicketType` (null for a `FreeEntry` visit, whose kind is in `TicketEventFreeEntries`).

```mermaid
erDiagram
    Event_node  ||--o| EventPrices : "has 0..1"
    Season_node ||--o| SeasonPrices : "has 0..1"
    EventPrices  ||--o{ EventPriceCategories : "n category rows"
    SeasonPrices ||--o{ SeasonPriceCategories : "n category rows"

    Orders |o--o{ EventTickets : "OrderId"
    Orders |o--o{ SeasonSingleTickets : "OrderId"
    Orders |o--o{ SeasonPasses : "OrderId"
    Orders |o--o{ MembershipCards : "OrderId"

    Event_node  ||--o{ EventTickets : "EventId"
    Season_node ||--o{ SeasonSingleTickets : "SeasonId"
    Season_node ||--o{ SeasonPasses : "SeasonId"
    Season_node ||--o{ MembershipCards : "SeasonId"
    Event_node  ||--o{ TicketEventVisits : "EventId"

    TicketEventVisits ||--o{ TicketEventVisitsLogs : "VisitId"
    TicketEventVisits ||--o| TicketEventFreeEntries : "VisitId (FreeEntry only)"

    Event_node {
        int Id PK "Umbraco content node"
    }
    Season_node {
        int Id PK "Umbraco content node"
    }

    Orders {
        int Id PK
        string OrderNumber UK
        string BillingFirstName
        string BillingLastName
        string BillingStreet
        string BillingAddressLine2 "null"
        string BillingPostalCode
        string BillingCity
        string BillingCountry
        string BillingEmail
        string BillingPhone "null"
        string Currency
        decimal SubtotalNet
        decimal VatRate
        decimal VatAmount
        decimal TotalGross
        string SellerUid "null"
        int PaymentMethod "enum PaymentMethod"
        int Status "enum OrderStatus"
        datetime CreatedAt
        datetime PaidAt "null"
    }

    EventTickets {
        int Id PK
        string Uuid UK
        int EventId "Umbraco event node"
        int Category "enum TicketCategory"
        decimal Price
        int OrderId FK "null"
        int Status "enum TicketStatus"
        datetime CreatedAt
        bool Redeemed
    }

    SeasonSingleTickets {
        int Id PK
        string Uuid UK
        int SeasonId "Umbraco season node"
        int Category "enum TicketCategory"
        decimal Price
        int OrderId FK "null"
        int Status "enum TicketStatus"
        datetime CreatedAt
        int RedeemedEventId "null; the event it was consumed at"
        bool Redeemed
    }

    SeasonPasses {
        int Id PK
        string Uuid UK
        int SeasonId "Umbraco season node"
        int Category "enum TicketCategory"
        decimal Price
        int OrderId FK "null"
        int Status "enum TicketStatus"
        datetime CreatedAt
    }

    MembershipCards {
        int Id PK
        string Uuid UK
        int SeasonId "Umbraco season node"
        int Category "enum TicketCategory"
        decimal Price
        int OrderId FK "null"
        int Status "enum TicketStatus"
        datetime CreatedAt
        string FirstName "null"
        string LastName "null"
        datetime Birthday "null"
    }

    TicketEventVisits {
        long Id PK
        int EventId "Umbraco event node"
        int TicketType "enum TicketType"
        string TicketUuid "null for FreeEntry; else ticket Uuid"
        bool IsInside "current presence"
        datetime CreatedAt
    }

    TicketEventVisitsLogs {
        long Id PK
        long VisitId FK
        int Type "enum VisitLogType (CheckIn/CheckOut)"
        datetime OccurredAt
        string ScannedBy "null"
    }

    TicketEventFreeEntries {
        long Id PK
        long VisitId FK "unique; one per FreeEntry visit"
        int FreeEntryType "enum FreeEntryType"
    }

    EventPrices {
        int Id PK
        int EventId UK "Umbraco event node"
        int TotalSalesQuota "null; Verkaufskontingent gesamt"
        int AdmissionQuota "null; Einlasskontingent (persons)"
    }

    EventPriceCategories {
        int Id PK
        int EventPriceId FK
        int Category "enum TicketCategory"
        decimal SalePrice
        int Quota "null; Kontingent per category"
    }

    SeasonPrices {
        int Id PK
        int SeasonId UK "Umbraco season node"
    }

    SeasonPriceCategories {
        int Id PK
        int SeasonPriceId FK
        int Category "enum TicketCategory"
        decimal SalePrice
        int Quota "null; Kontingent per category"
    }
```

Enum integer values (order defines the stored number):

| Enum | Values |
|------|--------|
| `TicketCategory` | 0 Adult, 1 AdultReduced, 2 Youth, 3 YouthReduced, 4 Child |
| `TicketType` | 0 EventTicket, 1 SeasonSingle, 2 SeasonPass, 3 MemberCard, 4 FreeEntry |
| `FreeEntryType` | 0 Player, 1 Staff, 2 Official, 3 SwissUnihockeyFreeCard |
| `OrderStatus` | 0 Draft, 1 Paid, 2 Cancelled, 3 Refunded |
| `TicketStatus` | 0 Valid, 1 Cancelled |
| `PaymentMethod` | 0 Payrexx, 1 Cash, 2 Twint, 3 Invoice |
| `VisitLogType` | 0 CheckIn, 1 CheckOut |

Availability for sale is resolved by `EventPricingReader`: a category is sold out once its own `Quota`, or the event's `TotalSalesQuota`, is reached by the valid `EventTickets` already issued. `AdmissionQuota` caps the number of admitted persons (tickets plus free entries).

## Documentation

- `ARCHITECTURE.md`: design, layering, content-slice model, the ticketing data model, code-first seeding, and the runtime-compiled Razor caveats.
- `CLAUDE.md` / `AGENTS.md`: working conventions for AI coding agents.

## Tech stack

Umbraco 17, .NET 10, SQLite, uSync, Sqids (opaque URL ids), Payrexx (payment), Brevo (email), Cloudflare Turnstile (captcha). Default culture is Swiss German (`de-CH`).
