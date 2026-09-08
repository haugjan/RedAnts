# Architecture plan, autumn 2026

Follow-up plan to the target architecture of 2026-09-04 (see `ARCHITECTURE.md`, section "Target architecture status and backlog"). Phases 0 to 9 of that plan are live on prod since 2026-09-08. This plan covers what the verification of 2026-09-08 left open, in the order the phases should land. Every phase is a feature branch, verified with the unit, architecture and browser tests against the agent database, deployed to dev first and to prod only on explicit approval. The port renaming that the verification listed as backlog item 5 is deliberately not part of this plan.

## Phase A: flat module folders and project-aligned namespaces

**Goal.** Each class library holds exactly one module, so the folder level `Domain/Ticketing`, `Features/Ticketing`, `Infrastructure/Ticketing` (and `…/Show`) disappears and the content moves one level up. Namespaces follow project plus path: `RedAnts.Ticketing.Domain.Sales`, `RedAnts.Ticketing.Features.CheckoutWorkflow`, `RedAnts.Ticketing.Infrastructure.Email`, `RedAnts.Show.Features.ShowWorkflow`. The module stays in the namespace, so Ticketing and Show never share a namespace. The Host keeps `Features/Website`, `Infrastructure/Website` and `Infrastructure/Shared` because it has a real subdivision.

**Steps.**
1. Move the folders with `git mv` (first commit, history follows the files).
2. Rename the namespaces in every `.cs`, `.razor`, `.cshtml`, `.csproj`, `.md` file with word-bounded replacements; set `RootNamespace` to `RedAnts.Ticketing` and `RedAnts.Show`.
3. Update the namespace regexes of the architecture tests (layers, module boundaries, handler rule) and prove they still fail on a violation.
4. Update the paths and namespaces quoted in `CLAUDE.md`, `ARCHITECTURE.md` and `README.md`.

**Verification.** Full Release build, all test projects, `/warmup` locally (runtime-compiled Host views are the place a missed `@using` hides), the 17 browser tests, then dev.

**Risk.** Mechanical but wide (about 350 files). The Host views are only compiled at runtime, so the warmup and browser checks are mandatory.

## Phase B: one component per dialog in the admin UI

**Goal.** `AdminFlexTicketsComponent` (1354 lines), `AdminTicketsComponent` (1210), `AdminMemberCardsComponent` (1112), `AdminSeasonCardsComponent` (1058) and `ShowAdminPage.razor` (1528) each carry several dialogs, their state and their handler calls. Every dialog becomes its own component with one command, the list component only holds the table, the filter and the query.

**Steps.**
1. Per component, list the dialogs (create, edit, import, mail, convert, delete, holder, status) and the fields each one owns.
2. Extract one dialog at a time into `Features/Admin/<Area>/<Dialog>.razor` with parameters for the selected row, an `OnSaved` callback and the try/catch around its single handler call; the list re-queries on `OnSaved`.
3. Keep the ESC-to-cancel behaviour and the `.ta-*` styles; no visual change.
4. Repeat for the Show admin page (profiles, settings, upload, Spotify search).

**Verification.** The `AdminCardsShould`, `AdminCatalogShould`, `AdminOrdersShould`, `AdminScanShould` and `ShowShould` browser tests plus screenshots of every dialog before and after; the smell report must drop below 600 lines for these files.

**Risk.** Behavioural drift in rarely used dialogs; mitigate by extracting one dialog per commit and running the browser tests each time.

## Phase C: scopes in handlers that write several tables

**Goal.** `PlaceOrder` with `OrderFulfillment`, `CreateAdminOrder`, `RefundOrder`, the ticket and pass imports and the card mailings write two or more tables. A failure in the second write must not leave a half-written order, ticket or refund.

**Steps.**
1. Add a small port `IUnitOfWork` in `Features/Ports` (`Task<T> RunAsync(Func<Task<T>> work)`) implemented in Infrastructure on Umbraco's `IScopeProvider` (scope with a transaction, complete on success).
2. Wrap the multi-table sections of the listed handlers; keep the capacity reservation release as the compensation for the reservation counters, which live outside the transaction on purpose (optimistic concurrency).
3. Add handler tests with a failing second repository to prove the rollback.

**Verification.** Handler tests, `CheckoutShould` and `AdminOrdersShould`, an order placed on dev.

**Risk.** Long-running transactions around Payrexx calls; the Payrexx call stays outside the scope, only the local writes are inside.

## Phase D: Check slices for rules that need I/O

**Goal.** Rules the Blazor components repeat today (can this cart check out, can this order be refunded, can this card be converted for this event) become `Check` slices returning `CheckResult.Allowed` or `Denied(reason)`, and the UI asks them instead of re-implementing them.

**Steps.**
1. `CanCheckout` (cart, capacity, express limits), `CanRefund` (status, remaining amount, Payrexx), `CanConvert` (conversion rules, quantity caps).
2. Commands keep their own guards (a handler never calls a handler) but share the rule code through domain methods on `Cart`, `Order` and the conversion rule resolver.
3. Components call the checks to enable or disable buttons and show the reason.

**Verification.** Handler tests per check, the checkout and refund browser tests.

## Phase E: value objects in the sales domain

**Goal.** `EmailAddress` and `Money` from the kernel replace raw strings and decimals in `Buyer`, `Cart`, `Order`, `OrderItem`, pricing and the refund amounts; format validation moves into the value objects (`ValidationException` with the field name), so controllers and components stop validating formats themselves.

**Steps.**
1. Introduce the value objects at the aggregate boundaries first (constructors and factories), keep NPoco records on primitives with explicit mapping.
2. Replace the format checks in `CheckoutController`, the admin dialogs and the import parsers by the value object constructors and let `DomainErrorFilter` and the component catches render the field message.
3. Extend the kernel tests for the accepted and rejected formats (Swiss francs with five-rappen rounding, e-mail formats).

**Verification.** Kernel and handler tests, checkout browser test, one admin import on dev.

## Phase F: testability and test infrastructure

**Goal.** Deterministic time and repository-level tests.

**Steps.**
1. Inject `TimeProvider` into the factories and expiry jobs (`DraftOrderExpiry`, `ExpireDraftOrders`, ticket token expiry) and read `SwissTime` through it in tests.
2. Add an NPoco test fixture in `RedAnts.Ticketing.Tests` that runs against `sqldb-redants-agent` when a connection string is present and skips otherwise, covering the repositories with the most SQL (`OrderRepository`, `FlexTicketBundleRepository`, the report readers).
3. Replace the machine path in `TicketPdfRendererTests` by a path resolved from the test assembly location.

**Verification.** The new tests green locally and in CI (CI skips the database fixture).

## Phase G: fail fast in the deploy-time migration

**Goal.** When the stored plan state of a database is unknown to the deployed plan, the migrate step reports one clear line (database, stored state, last known state) and fails, instead of an exit code 134 and a stack trace, and the deploy summary shows it.

**Steps.**
1. In `TicketingMigrationComponent` compare the stored state against the plan's transitions before `ExecuteAsync`; log the diagnosis and throw a `DomainException` with the same text.
2. In `deploy.yml` print the migrate log tail into the job summary on failure.

**Verification.** A dev deploy against a deliberately foreign state on the agent database.
