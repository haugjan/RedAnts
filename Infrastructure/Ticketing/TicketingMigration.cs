using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Core.Composing;
using RedAnts.Infrastructure.Ticketing.Sales;

namespace RedAnts.Infrastructure.Ticketing;

// Schema plan: CreateTicketingSchema builds the full current schema for a fresh database. Additive
// changes are appended as further, idempotent steps so an existing (dev) database upgrades in place
// and does NOT need to be dropped.
public class TicketingMigrationPlan : MigrationPlan
{
    public TicketingMigrationPlan() : base("Ticketing")
    {
        From(string.Empty).To<CreateTicketingSchema>("ticketing-schema");
        // Adds the Flexticket bundle table + SeasonSingleTickets.BundleId to existing databases.
        To<AddFlexTicketBundles>("ticketing-flextickets");
    }
}

/// <summary>Creates the full ticketing schema at once: the immutable Order plus one table per ticket
/// type, the admissions (visits + in/out scan logs + free-entry detail), and the pricing catalog
/// (per-event and per-season price sets, each with its category rows). Catalog entities
/// (Season/Venue/Event) are Umbraco Document Types, not custom tables.</summary>
public class CreateTicketingSchema(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        // Sales: order (Bestellung) + issued tickets per type + admissions
        Create.Table<OrderRecord>().Do();
        Create.Table<EventTicketRecord>().Do();
        Create.Table<SeasonSingleTicketRecord>().Do();
        Create.Table<SeasonPassRecord>().Do();
        Create.Table<MemberCardRecord>().Do();
        Create.Table<EventVisitRecord>().Do();
        Create.Table<EventVisitLogRecord>().Do();
        Create.Table<EventFreeEntryRecord>().Do();

        // Flexticket bundles: batches of season single tickets, identified by a per-season reference
        Create.Table<FlexTicketBundleRecord>().Do();

        // Pricing catalog: per-event and per-season price sets (parent + n category sub-rows)
        Create.Table<EventPriceRecord>().Do();
        Create.Table<EventPriceCategoryRecord>().Do();
        Create.Table<SeasonPriceRecord>().Do();
        Create.Table<SeasonPriceCategoryRecord>().Do();

        return Task.CompletedTask;
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
        await upgrader.ExecuteAsync(migrationPlanExecutor, scopeProvider, keyValueService);
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
