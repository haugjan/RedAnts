# Architecture plan, autumn 2026

Follow-up plan to the target architecture of 2026-09-04 (see `ARCHITECTURE.md`, section "Target architecture status and backlog"). Phases 0 to 9 of that plan are live on prod since 2026-09-08. This plan covers what the verification of 2026-09-08 left open plus three directions given on 2026-09-08: vertical feature folders with ports and views inside the slices, no technical collection folders, and separate command and read models. Phases land in the order below. Every phase is a feature branch, verified with the unit, architecture and browser tests against the agent database, deployed to dev first and to prod only on explicit approval. No phase starts without an explicit go. The port renaming that the verification listed as backlog item 5 is not a phase of its own: it happens as a by-product of Phase B, where every collection port is split into its write side and its read sides.

## Phase A: feature folders, ports and views inside the slices, project-aligned namespaces

**Done 2026-09-09**, commits `d77c31e` (flatten and rename), `e2b2fae` (capability folders), `db14a68` (views into the features) and the fourth commit of branch `feature/s6-architecture-plan` (architecture tests and docs). Deviations from the tree below: `IEventPricing` sits in `Checkout` (its slices use it), `MyTicketsController` and `MyTicketTokenSigner` never existed, `WebTicketEvents.cshtml` joined `Tickets/Views`, the Show admin view is `Features/Admin/Views/ShowAdmin.cshtml` because two libraries cannot compile a view on the same path, and the Show module carries its own copy of `FeatureViewLocationExpander` because the modules share no ASP.NET assembly.

**Goal.** Each class library holds exactly one module, so the folder level `Domain/Ticketing`, `Features/Ticketing`, `Infrastructure/Ticketing` (and `…/Show`) disappears. Inside `Features/` the code is cut vertically by capability instead of by technical kind: one folder per capability holding its slices, its ports (one interface per file, no `Ports` folder), its admin UI, its MVC views and its adapters. Very large folders are split into sub-features; `Admin` (71 files) and `CardWorkflow` (38 files) dissolve into the capabilities they serve, and the admin shell keeps only the frame and the shared widgets. Namespaces follow project plus path (`RedAnts.Ticketing.Features.Checkout`), so the module stays in the namespace and Ticketing and Show never share one. The Host keeps `Features/Website`, `Infrastructure/Website` and `Infrastructure/Shared`.

**Target tree (Ticketing).**

```
src/RedAnts.Ticketing/
  Domain/                 Sales/, Admission/ (aggregates and domain services, unchanged)
  Features/
    Admin/                shell only: TicketingAdminComponent, TicketingAdminController, manifest, state,
                          ConfirmDialog, DateBox, Inline*Edit, LinkOverlay, PersonCell, PersonFields, PersonForm,
                          AdminFormat, AdminName, AdminIdentity, SpreadsheetReader, TicketTypeDisplay, Views/Admin.cshtml
    Catalog/              11 slices, IEvents, ISeasons, IVenues, IEventPrices, ISeasonPrices, IPriceTiers, ISeasonAddOns,
                          IEventConversionRules, IEventQuotasReader, IContentUrls, IEventStatusPublisher, ISeasonStatusPublisher
                          Admin/ AdminEventsComponent, AdminSeasonsComponent, AdminAddOnsComponent, EventLinks, SeasonLinks, SeasonStats
                          Infrastructure/ SalesCatalogRepositories, EventConversionRulesRepository, UmbracoCatalogReaders,
                          UmbracoContentUrls, Umbraco*StatusPublisher, EventQuotasReader, *LinkReader, SeasonStatsReader, ArticleGuids
    Checkout/             13 slices, steps CapacityReservation and OrderFulfillment, ICartRepository, IOrderTokens,
                          IPayrexxGateway, ICaptchaVerifier, IOrderMailer, CartController, CheckoutController, CheckoutModels
                          Views/ Cart/Index, Checkout/Address, Express, Processing, Confirmation, Cancelled, _Summary
                          Infrastructure/ SessionCartRepository, DataProtectionOrderTokens, PayrexxGateway, TurnstileVerifier,
                          DraftOrderExpiry, OrderMailer, MailTicketActions, TicketDeliveryComposer
    Orders/               ChangeOrderStatus, RefundOrder, CreateAdminOrder, IOrders, IOrderItems, IOrderRefunds, IOrderLog,
                          IOrderTickets, IOrderAddOns
                          Admin/ AdminOrdersComponent, OrderItemsOverlay, OrderLogOverlay, RefundOverlay, OrderAdminReport, OrderAddOnAdminReport
                          Infrastructure/ OrderRepository, OrderItemRepository, OrderRefundRepository, OrderLogRepository,
                          OrderAddOnRepository, OrderTicketDeactivator, Order*ReportReader, AccountingJournalBackfill, OrderItemsBackfill
    Admission/            ScanTicket, ScanCode, GrantFreeEntry, RevokeFreeEntry, GetOccupancy, step TicketScanning,
                          IAdmissionRepository, IFreeEntryRepository, IOccupancyReader, IAdmissionFactsReader, ITicketRedemptions,
                          TicketScanner, ScanView, RebookView, BoxOfficeView, HelperSessionCookie, ScanController, ScannerTestController
                          Views/ ScanTickets, ScannerTest
                          Admin/ AdminFreeEntriesComponent, FreeEntryAdminReport, VisitsOverlay, VisitLog, EventAdmissionReport
                          Infrastructure/ AdmissionRepository, FreeEntryRepository, OccupancyReader, AdmissionFactsReader,
                          TicketRedemptions, VisitLogRows, FreeEntryQuotaMapping, FreeEntryQuotas, VisitLogReader, *ReportReader
    Tickets/              event-ticket slices (issue, edit, delete, mail, print), IEventTickets, ITicketPdf, ITicketTokens,
                          IMyTicketTokens, IMyTicketsReader, IPublicBaseUrl, ITicketPrinter, ITicketPrintSettings, IEventTicketMailer,
                          IAdminTicketDeletion, WebTicketController, MyTicketsController, TicketCardModel, TicketDisplay
                          Views/ WebTicket, MyTickets/Index, Partials/_TicketCard
                          Admin/ AdminTicketsComponent, TicketPrintDialog, TicketPrintController, EventTicketImportController, TicketImportCsv, TicketExportCsv
                          Infrastructure/ EventTicketRepository, TicketPdfRenderer, QrCodeRenderer, TicketTokenSigner, MyTicketTokenSigner,
                          MyTicketsReader, IssuedTicketReader, PublicBaseUrl, TicketPrinting, TicketPrintSettingsRepository, Fonts/,
                          FlexPrintFontResolver, EventTicketMailer, AdminTicketDeletion, TicketCode
    SeasonPasses/         slices, ISeasonPasses, ISeasonPassMailer, ISeasonPassPricing
                          Admin/ AdminSeasonCardsComponent, SeasonPassAdminReport, SeasonPassImportController, SeasonPassExportController
                          Infrastructure/ SeasonPassRepository, SeasonPassMailer, SeasonPassAdminReportReader
    MemberCards/          slices, IMemberCards, IMemberCardMailer, IConvertibleCards
                          Admin/ AdminMemberCardsComponent, MemberCardAdminReport, MemberImportController, MemberExportController
                          Infrastructure/ MemberCardRepository, MemberCardMailer, MemberCardAdminReportReader, ConvertibleCardResolver, MemberCardOrderCleanup
    FlexTickets/          slices, IFlexTicketBundles, IFlexTicketMailer
                          Admin/ AdminFlexTicketsComponent, FlexBundleExport, FlexBundleExportController, FlexImportController
                          Infrastructure/ FlexTicketBundleRepository, FlexTicketRecords, FlexTicketMailer, FlexBundleTicketsReader
    EventBundles/         slices, IEventTicketBundles
                          Admin/ EventBundleExport, EventBundleExportController
                          Infrastructure/ EventTicketBundleRepository, EventTicketBundleRecords, EventBundleTicketsReader
    Helpers/              AddHelperToSeason, RemoveHelperFromSeason, InviteHelperByMail, SetHelperActive, AssignHelperEvents,
                          IHelpers, IHelperInviteMailer, IHelperScanReport
                          Admin/ AdminHelpersComponent, HelperScanReport
                          Infrastructure/ HelperMemberRepository, HelperInviteMailer, HelperScanReportReader, HelperMemberTypeSeeder
    Newsletter/           INewsletterSignups, the signup slice
                          Admin/ AdminNewsletterComponent, NewsletterExportController, NewsletterFairgateCsv
                          Infrastructure/ NewsletterSignupRepository
    Stats/                the report ports; Admin/ AdminStatsComponent, StatsReports, VisitStatsReports; Infrastructure/ StatsReaders, VisitStatsReaders
    Email/                IEmailOutbox, IEmailSender, ITicketingMailSettings, IAddOnNotifier; Admin/ AdminOutboxComponent
                          Infrastructure/ OutboxRepository, OutboxRecords, OutboxDispatcher, OutboxEnqueuer, OutboxSignal, EmailComposer,
                          EmailTransportSelector, GraphEmailTransport, UmbracoEmailBridge, AddOnNotifier, EmailTestController, TicketingMailSettings
    Public/               NextController, WarmupController, AccessGate, DisplayCulture; Views/ NextEventEmbed, NextQuickBuy
                          Infrastructure/ PageViewTracker, PageViewRecord, PageViewPurge
    Shared/               MoneyFormat, EmailLayout, MailMarkdown; Views/ Shared/_TicketsLayout, _ViewImports
    TicketingFeatures.cs  the handler registration map (unchanged)
  Infrastructure/         boot and cross-cutting only: TicketingComposer, TicketingServiceCollectionExtensions,
                          TicketingApplicationBuilderExtensions, TicketingMigration, SessionCacheSchema, TicketingMappers,
                          Content/ TicketingContentTypeSeeder, TicketingAliases, TicketingContentDefaults, PriceTierSeeder,
                          SaisonsContentFinder, EventPriceDefaults
  Pages/                  removed: ScanTickets becomes a view in Admission
  Views/                  removed: every view lives in its feature, the layout and _ViewImports in Features/Shared/Views
```

Show follows the same rules with the capabilities `Board/` (ShowController, ShowBoardComponent, ShowLayout, ShowJson, ShowConfig, GetShowProfiles, IShowProfiles, Views/Index), `Admin/` (ShowAdminController, ShowAdminPage, ShowAdminManifestReader, ShowEditModels, SaveShowProfiles, SetShowSetting, SearchSpotify, IShowSettings, Views/Admin), `Remote/` (ShowApiController, DispatchShowCommand, ShowRemote) and `Sounds/` (ShowSoundController, ShowSoundDelivery, ShowStorageOptions, UploadShowSound, ShowSoundUploader). Its adapters may stay in the top-level `Infrastructure/` (ShowDatabase, ShowSchema, ShowMigrationComponent, repositories, ShowSpotifySearch) while that folder holds fewer than 15 files.

**Rules after the move (go into CLAUDE.md and ARCHITECTURE.md).**

1. A feature folder is one capability. Its root holds the slices (one file each) and its ports (`I….cs`, one interface per file). There is no `Ports` folder and no `*Workflow` folder.
2. `Admin/` holds the Blazor components, dialogs, reports and import/export controllers of that capability. `Views/` holds its MVC views. `Infrastructure/` holds its adapters (repositories, readers, mailers, clients); their namespace ends in `.Infrastructure` and the layer rule treats every namespace with an `.Infrastructure` segment as infrastructure. An adapter lives with the port it implements.
3. The top-level `Infrastructure/` keeps only boot, migrations, content seeding, mappers and the mail transports.
4. A folder that passes about 25 files is split into sub-features (`Tickets/Print/`, `Catalog/Pricing/`); the admin split is the model.
5. Test folders mirror the feature folders (`tests/RedAnts.Ticketing.Tests/Checkout/`).
6. MVC views are found by `FeatureViewLocationExpander`: a controller in `RedAnts.Ticketing.Features.<Feature>` resolves `/Features/<Feature>/Views/{1}/{0}.cshtml`, `/Features/<Feature>/Views/{0}.cshtml` and `/Features/Shared/Views/{0}.cshtml`. Umbraco templates stay in the Host `Views/` folder because Umbraco owns them.

**Steps (four commits on one branch).**

1. Flatten the module folder with `git mv` and rename the namespaces to `RedAnts.Ticketing.*` / `RedAnts.Show.*` with word-bounded replacements; set `RootNamespace` to `RedAnts.Ticketing` and `RedAnts.Show`.
2. Build the capability folders: move slices, ports, admin UI and adapters as listed, delete `Ports`, the `*Workflow` folders, the technical `Admin` and `Infrastructure/<Area>` folders; namespaces follow the folders; `TicketingFeatures.Handlers` and the composer registrations change only in their usings.
3. Move the views: add `FeatureViewLocationExpander` (registered in `AddTicketing`, and in `AddShow` for the Show views), move every `.cshtml` into its feature, convert `Pages/ScanTickets.cshtml` into `ScanController` plus `Features/Admission/Views/ScanTickets.cshtml` (the scan middleware and the `Server` render mode stay), remove `Pages/` and the root `Views/`.
4. Architecture tests: layers by namespace segment (Domain, Features without `.Infrastructure`, Infrastructure), module boundaries `RedAnts.Ticketing.*` / `RedAnts.Show.*`, handler rule `RedAnts.<Module>.Features.<Feature>(.<Sub>)?` excluding `.Admin`, `.Views` and `.Infrastructure`, a new rule that only the composer and the registration extensions reference a feature's `Infrastructure` namespace; prove each rule with a deliberate violation before committing. Then the docs: `CLAUDE.md` (repository layout, the rules above), `ARCHITECTURE.md` (layering section, rationale for feature folders and the view expander), `README.md`.

**Verification.** Full Release build without incremental state, all test projects, local `/warmup` with every path at 200 (the view expander and the runtime-compiled Host views are the places a missed `@using` or a wrong view path hides), the 17 browser tests including the scanner login and the Show board and admin pages, `dotnet publish` of the Host to check that the compiled views of the class libraries resolve from their new paths, then dev.

**Risk.** Mechanical but wide (about 500 files); the view lookup is the one functional change, so the warmup and browser checks are mandatory. Easy to miss: `Program.cs`, `_ViewImports.cshtml`, `typeof(…TicketScanner)` in the scan view, both `_Imports.razor`, `[ComposeAfter]` attributes, the Umbraco backoffice manifest paths for the admin section.

## Phase B: separate command and read models (CQRS inside one database)

**Goal.** Every write runs through a command slice that loads one aggregate through `I<Aggregate>Repository` (load, save, delete; nothing else) and every read runs through a query slice that returns a read model, an immutable record shaped for one screen, through an `I…Reader` port with SQL written for that screen. Components, controllers and views inject handlers only; ports are used by handlers and adapters. Same tables, no second store: the separation is in the code, not in the storage.

**Today.** 70 commands and 6 queries. The admin components inject the report ports (`OrderAdminReport`, `SeasonPassAdminReport`, `MemberCardAdminReport`, …) and the collection ports (`IEvents`, `IOrders`, `IMemberCards`, …) directly, the public views inject `IEventPricing`, `ISeasons` and `ICartRepository`, and the collection ports mix `SearchAsync` and `GetAllAsync` with `SaveAsync`.

**Steps.**
1. Per capability, sort every port method into the write side (load, save, delete by id or uuid) and the read side (lists, searches, reports, counts, lookups for screens). The write side becomes `I<Aggregate>Repository`; the read side becomes one `I…Reader` per screen or export with its read model record next to it (`Orders/OrderListRow.cs`, `MemberCards/MemberCardRow.cs`).
2. Add the query slices the screens need: `GetOrdersForAdmin`, `GetEventTicketsForAdmin`, `GetSeasonPasses`, `GetMemberCards`, `GetFlexBundles`, `GetEventBundles`, `GetHelpers`, `GetFreeEntries`, `GetNewsletterSignups`, `GetOutbox`, `GetSeasonStats`, `GetVisitorStats`, `GetMyTickets`, `GetEventForSale`, `GetSeasonForSale`, `GetNextEvent`, `GetCart`; the CSV exports reuse them.
3. Rewire the components, controllers and views to handlers only and delete the direct port injections; the Umbraco templates in the Host use the public query handlers through `@inject`.
4. Architecture rules: types in `.Admin`, `.Views`, controllers and Razor components must not depend on any port interface (`I…Repository`, `I…Reader`, mailers); handlers are the only entry. A second rule: `I<Aggregate>Repository` interfaces expose no list or search methods.
5. Tests: one handler test per query with a faked reader; the fakes in the test project split the same way.

**Verification.** Architecture rules, handler tests, browser tests; in the smell report the injection counts of the components fall below 7.

**Risk.** Wide but mechanical per capability: the reads keep their SQL, only the entry point moves. One capability per commit, dev deploy per capability. Dependency: after A, before C, so the list components are already thin when the dialogs move out.

**Done 2026-09-09.** Commits `5c4bdf1` (group 1: Catalog, Checkout, Public, public Tickets part), `b553e87` (group 3: SeasonPasses, MemberCards, FlexTickets, EventBundles, Helpers), `ae6fbfd` (group 2: Orders, Admission, admin Tickets part, Stats, Email, Newsletter) and `69658c5` (integration: handlers only in the UI, repositories without list methods, the rules `UiRules.Ui_depends_on_handlers_only` and `PortRules.Repositories_expose_no_collections` with their empty baselines). Deviations from the steps above: the statistics tab has one query per block (`GetSeasonStats`, `GetSalesStats`, `GetEventStats`, `GetVisitorStats`) instead of `GetSalesFunnel`; search, filter and paging run inside the queries (`GetOrdersForAdmin`, `GetEventTicketsForAdmin`, `GetOrderAddOnsForAdmin`) rather than in the components; the CSV exports keep their routes (`/admin/members/season/{id}/cards.csv`, `/admin/season-passes/season/{id}/passes.csv`, `/admin/flex-tickets/bundles.csv`, `/admin/event-tickets/tickets.csv`) and reuse the screen queries, so group 2's `GetTicketsForExport` was dropped in favour of `GetEventBundlesForExport`; season pickers use `GetSeasonChoices` (`SeasonChoice` with an `IsCurrent` flag) instead of `GetSeasonsForAdmin`; `IPriceTierRepository.LoadSeasonAsync` and `ISeasonAddOnRepository.LoadSeasonAsync` return the season's tier set and add-on set (`SeasonPriceTiers`, `SeasonAddOnSet`) because the pricing commands edit them as one unit; the event conversion rules got a separate `IEventConversionRuleReader`; `GetOrderConfirmation` reads through `IOrderConfirmationReader`; the Show board subscribes to remote commands through `ListenForShowCommands`, and the Spotify and sound-bundle needs of the Show admin page and the two sound controllers became `GetSpotifySettings`, `TestSpotifyCredentials`, `LookupSpotifyReference`, `DownloadShowSound`, `RestoreShowSound` and `OpenShowSound`; the development controllers (`/scanner-test`, `/dev/test-mail`, `/dev/ticket-mail-preview`) use `GetScannerTestCards`, `SendTestMail` and `SendOrderMailSample`. The smell-report target (fewer than 7 injections per component) is not met yet: the list components still inject one handler per dialog, which Phase C removes. Test state: Kernel 31, Show 19, Host 5, Ticketing 586, Architecture 27 (none skipped), Browser 25.

## Phase C: one component per dialog in the admin UI

**Goal.** `AdminFlexTicketsComponent` (1354 lines), `AdminTicketsComponent` (1210), `AdminMemberCardsComponent` (1112), `AdminSeasonCardsComponent` (1058) and `ShowAdminPage.razor` (1528) each carry several dialogs, their state and their handler calls. Every dialog becomes its own component with one command, the list component only holds the table, the filter and the query from Phase B.

**Steps.**
1. Per component, list the dialogs (create, edit, import, mail, convert, delete, holder, status) and the fields each one owns.
2. Extract one dialog at a time into `Features/<Capability>/Admin/<Dialog>.razor` with parameters for the selected row, an `OnSaved` callback and the try/catch around its single handler call; the list re-queries on `OnSaved`.
3. Keep the ESC-to-cancel behaviour and the `.ta-*` styles; no visual change.
4. Repeat for the Show admin page (profiles, settings, upload, Spotify search) in `Features/Admin/` of the Show project.

**Verification.** The `AdminCardsShould`, `AdminCatalogShould`, `AdminOrdersShould`, `AdminScanShould` and `ShowShould` browser tests plus screenshots of every dialog before and after; the smell report must drop below 600 lines for these files.

**Risk.** Behavioural drift in rarely used dialogs; mitigate by extracting one dialog per commit and running the browser tests each time.

## Phase D: scopes in handlers that write several tables

**Goal.** `PlaceOrder` with `OrderFulfillment`, `CreateAdminOrder`, `RefundOrder`, the ticket and pass imports and the card mailings write two or more tables. A failure in the second write must not leave a half-written order, ticket or refund.

**Steps.**
1. Add a small port `IUnitOfWork` next to the checkout slices (`Task<T> RunAsync(Func<Task<T>> work)`) implemented in `Features/Checkout/Infrastructure` on Umbraco's `IScopeProvider` (scope with a transaction, complete on success).
2. Wrap the multi-table sections of the listed handlers; keep the capacity reservation release as the compensation for the reservation counters, which live outside the transaction on purpose (optimistic concurrency).
3. Add handler tests with a failing second repository to prove the rollback.

**Verification.** Handler tests, `CheckoutShould` and `AdminOrdersShould`, an order placed on dev.

**Risk.** Long-running transactions around Payrexx calls; the Payrexx call stays outside the scope, only the local writes are inside.

## Phase E: Check slices for rules that need I/O

**Goal.** Rules the Blazor components repeat today (can this cart check out, can this order be refunded, can this card be converted for this event) become `Check` slices returning `CheckResult.Allowed` or `Denied(reason)`, and the UI asks them instead of re-implementing them.

**Steps.**
1. `CanCheckout` (cart, capacity, express limits), `CanRefund` (status, remaining amount, Payrexx), `CanConvert` (conversion rules, quantity caps).
2. Commands keep their own guards (a handler never calls a handler) but share the rule code through domain methods on `Cart`, `Order` and the conversion rule resolver.
3. Components call the checks to enable or disable buttons and show the reason.

**Verification.** Handler tests per check, the checkout and refund browser tests.

## Phase F: value objects in the sales domain

**Goal.** `EmailAddress` and `Money` from the kernel replace raw strings and decimals in `Buyer`, `Cart`, `Order`, `OrderItem`, pricing and the refund amounts; format validation moves into the value objects (`ValidationException` with the field name), so controllers and components stop validating formats themselves.

**Steps.**
1. Introduce the value objects at the aggregate boundaries first (constructors and factories), keep NPoco records on primitives with explicit mapping.
2. Replace the format checks in `CheckoutController`, the admin dialogs and the import parsers by the value object constructors and let `DomainErrorFilter` and the component catches render the field message.
3. Extend the kernel tests for the accepted and rejected formats (Swiss francs with five-rappen rounding, e-mail formats).

**Verification.** Kernel and handler tests, checkout browser test, one admin import on dev.

## Phase G: testability and test infrastructure

**Goal.** Deterministic time and repository-level tests.

**Steps.**
1. Inject `TimeProvider` into the factories and expiry jobs (`DraftOrderExpiry`, `ExpireDraftOrders`, ticket token expiry) and read `SwissTime` through it in tests.
2. Add an NPoco test fixture in `RedAnts.Ticketing.Tests` that runs against `sqldb-redants-agent` when a connection string is present and skips otherwise, covering the repositories with the most SQL (`OrderRepository`, `FlexTicketBundleRepository`, the report readers).
3. Replace the machine path in `TicketPdfRendererTests` by a path resolved from the test assembly location.

**Verification.** The new tests green locally and in CI (CI skips the database fixture).

**Done 2026-09-09.** Commits `e1f9e8e` (TimeProvider in `AddTicketing`, `SwissTime.TimestampOf/NowOf/TodayOf`, the sales factories, `DraftOrderExpiry`, `ExpireDraftOrders.Command.Due`, `OutboxDispatcher`, `TicketTokenSigner`), `4b1f0a9` (`Database/AgentDatabaseFixture.cs` with `AgentScopeProvider`, `AgentDatabase`, `[DatabaseFact]` and twelve repository and reader tests) and `96e7ffa` (the ticket PDF web root resolved from the test assembly). Deviations from the steps above: the overloads are named `TimestampOf`/`NowOf`/`TodayOf` because C# forbids a method and a property of the same name, the factories take a trailing optional `TimeProvider? time = null` so no call site in the checkout and import handlers had to change while Phases C and D were running, and `IOrderTokens` has no expiry to inject a clock into. The covered readers are the order list reader and the season pass list reader (the stats readers need seeded Umbraco content, which the fixture does not create). Test state: Kernel 34, Show 19, Host 5, Ticketing 603 with the agent database and 591 with 12 skipped without it, Architecture 27, Browser 25 (not run in this phase).

## Phase H: fail fast in the deploy-time migration

**Goal.** When the stored plan state of a database is unknown to the deployed plan, the migrate step reports one clear line (database, stored state, last known state) and fails, instead of an exit code 134 and a stack trace, and the deploy summary shows it.

**Steps.**
1. In `TicketingMigrationComponent` compare the stored state against the plan's transitions before `ExecuteAsync`; log the diagnosis and throw a `DomainException` with the same text.
2. In `deploy.yml` print the migrate log tail into the job summary on failure.

**Verification.** A dev deploy against a deliberately foreign state on the agent database.
