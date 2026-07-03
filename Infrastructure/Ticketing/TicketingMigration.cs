using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Core.Composing;
using RedAnts.Infrastructure.Ticketing.Sales;

namespace RedAnts.Infrastructure.Ticketing;

// Idempotent schema build: every table (and additive column) is created only when it is missing, and
// the plan is re-run on every boot regardless of the recorded migration state (the state is reset
// before each run, see TicketingMigrationComponent). A new table therefore appears on the next start
// with no database drop, and the "recorded state says applied, but the table is gone" desync — which
// happens when the dev database is swapped or reset between parallel sessions — cannot occur.
public class TicketingMigrationPlan : MigrationPlan
{
    public TicketingMigrationPlan() : base("Ticketing")
    {
        From(string.Empty).To<CreateTicketingSchema>("ticketing-schema");
        // Adds the Flexticket bundle table + SeasonSingleTickets.BundleId to existing databases.
        To<AddFlexTicketBundles>("ticketing-flextickets");
    }
}

/// <summary>Ensures the full ticketing schema exists (idempotent, guarded per table): the immutable
/// Order plus one table per ticket type, the admissions (visits + in/out scan logs + free-entry
/// detail), the Flexticket bundles, and the pricing catalog (per-event and per-season price sets, each
/// with its category rows). Each table is created only when missing, so this is a safe no-op for
/// anything already present and can run on every boot. Catalog entities (Season/Venue/Event) are
/// Umbraco Document Types, not custom tables.</summary>
public class CreateTicketingSchema(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        // Sales: order (Bestellung) + issued tickets per type + admissions
        EnsureTable<OrderRecord>("Orders");
        EnsureTable<EventTicketRecord>("EventTickets");
        EnsureTable<SeasonSingleTicketRecord>("SeasonSingleTickets");
        EnsureTable<SeasonPassRecord>("SeasonPasses");
        EnsureTable<MemberCardRecord>("MembershipCards");
        EnsureTable<EventVisitRecord>("TicketEventVisits");
        EnsureTable<EventVisitLogRecord>("TicketEventVisitsLogs");
        EnsureTable<EventFreeEntryRecord>("TicketEventFreeEntries");

        // Flexticket bundles: batches of season single tickets, identified by a per-season reference
        EnsureTable<FlexTicketBundleRecord>("FlexTicketBundles");

        // Pricing catalog: per-event and per-season price sets (parent + n category sub-rows)
        EnsureTable<EventPriceRecord>("EventPrices");
        EnsureTable<EventPriceCategoryRecord>("EventPriceCategories");
        EnsureTable<SeasonPriceRecord>("SeasonPrices");
        EnsureTable<SeasonPriceCategoryRecord>("SeasonPriceCategories");

        return Task.CompletedTask;
    }

    /// <summary>Create the table only when it is missing, so the step is a safe no-op for tables that
    /// already exist and the whole migration can run on every boot.</summary>
    private void EnsureTable<T>(string tableName) where T : class
    {
        if (!TableExists(tableName)) Create.Table<T>().Do();
    }
}

/// <summary>Additive upgrade for existing databases: creates the Flexticket bundle table and adds the
/// SeasonSingleTickets.BundleId link when they are missing. Idempotent, so it is a no-op on a fresh
/// database where CreateTicketingSchema already created both.</summary>
public class AddFlexTicketBundles(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists("FlexTicketBundles"))
            Create.Table<FlexTicketBundleRecord>().Do();

        if (!ColumnExists("SeasonSingleTickets", "BundleId"))
            Alter.Table("SeasonSingleTickets").AddColumn("BundleId").AsInt32().Nullable().Do();

        return Task.CompletedTask;
    }
}

public class TicketingMigrationComponent(
    ICoreScopeProvider scopeProvider,
    IMigrationPlanExecutor migrationPlanExecutor,
    IKeyValueService keyValueService,
    IRuntimeState runtimeState) : IAsyncComponent
{
    public async Task InitializeAsync(bool isMainDom, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < RuntimeLevel.Run) return;

        var upgrader = new Upgrader(new TicketingMigrationPlan());

        // Forget the recorded migration state so the fully existence-guarded plan re-runs on every boot
        // and (re)creates any missing table or column. This makes the schema build idempotent and immune
        // to the migration-state/database desync that a plan-gated schema suffers when the dev database
        // is swapped or reset between parallel sessions.
        keyValueService.SetValue(upgrader.StateValueKey, string.Empty);

        await upgrader.ExecuteAsync(migrationPlanExecutor, scopeProvider, keyValueService);
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
