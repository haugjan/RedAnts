using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Core.Composing;
using RedAnts.Infrastructure.Ticketing.Sales;

namespace RedAnts.Infrastructure.Ticketing;

public class TicketingMigrationPlan : MigrationPlan
{
    public TicketingMigrationPlan() : base("Ticketing")
    {
        From(string.Empty)
            .To<CreateTicketingTablesV1>("v1.0.0")
            .To<DropCatalogTablesV2>("v1.1.0")
            .To<CreateSalesTablesV3>("v1.2.0")
            .To<DropLegacyTicketTablesV4>("v1.3.0");
    }
}

// v1.0.0 — ticket tables only. Catalog entities (Season/Venue/Event) are Umbraco Document Types,
// not custom tables.
public class CreateTicketingTablesV1(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists("SingleTickets")) Create.Table<SingleTicketRecord>().Do();
        if (!TableExists("SeasonTickets")) Create.Table<SeasonTicketRecord>().Do();
        if (!TableExists("MemberCards")) Create.Table<MemberCardRecord>().Do();
        if (!TableExists("TicketScanLog")) Create.Table<TicketScanLogRecord>().Do();
        return Task.CompletedTask;
    }
}

// v1.1.0 — the catalog moved to Umbraco Document Types; drop the old NPoco catalog tables.
public class DropCatalogTablesV2(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (TableExists("EventPrices")) Delete.Table("EventPrices").Do();
        if (TableExists("Events")) Delete.Table("Events").Do();
        if (TableExists("Seasons")) Delete.Table("Seasons").Do();
        if (TableExists("Venues")) Delete.Table("Venues").Do();
        return Task.CompletedTask;
    }
}

// v1.2.0 — the real ticket-scanning data model: an immutable Order (Bestellung) plus one table per
// ticket type, and a shared event-visit sub-table for the multi-event types (SeasonPass / MemberCard).
public class CreateSalesTablesV3(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists("Orders")) Create.Table<OrderRecord>().Do();
        if (!TableExists("EventTickets")) Create.Table<EventTicketRecord>().Do();
        if (!TableExists("SeasonSingleTickets")) Create.Table<SeasonSingleTicketRecord>().Do();
        if (!TableExists("SeasonPasses")) Create.Table<SeasonPassRecord>().Do();
        if (!TableExists("MembershipCards")) Create.Table<Sales.MemberCardRecord>().Do();
        if (!TableExists("TicketEventVisits")) Create.Table<EventVisitRecord>().Do();
        return Task.CompletedTask;
    }
}

// v1.3.0 — the old single/season ticket + member card + scan-log model was removed in favour of the
// new Sales model (Orders + per-type ticket tables). Drop the now-unused legacy tables.
public class DropLegacyTicketTablesV4(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (TableExists("TicketScanLog")) Delete.Table("TicketScanLog").Do();
        if (TableExists("SingleTickets")) Delete.Table("SingleTickets").Do();
        if (TableExists("SeasonTickets")) Delete.Table("SeasonTickets").Do();
        if (TableExists("MemberCards")) Delete.Table("MemberCards").Do();
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
