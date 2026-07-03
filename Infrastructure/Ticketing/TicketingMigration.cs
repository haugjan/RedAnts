using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Core.Composing;
using RedAnts.Infrastructure.Ticketing.Sales;

namespace RedAnts.Infrastructure.Ticketing;

// Single-step schema: the migration creates the current ticketing tables directly. There are no
// historical/intermediate steps — the app installs a fresh database, so there is nothing to upgrade
// from (a pre-existing dev DB must be deleted to re-create it cleanly).
public class TicketingMigrationPlan : MigrationPlan
{
    public TicketingMigrationPlan() : base("Ticketing")
        => From(string.Empty).To<CreateTicketingSchema>("ticketing-schema");
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
