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
        From(string.Empty).To<CreateTicketingSchema>("ticketing-schema");
        To<AddFlexTicketBundles>("ticketing-flextickets");
        To<AdjustMemberCardColumns>("membercards-reference-noprice");
    }
}

public class CreateTicketingSchema(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        EnsureTable<OrderRecord>("Orders");
        EnsureTable<EventTicketRecord>("EventTickets");
        EnsureTable<SeasonSingleTicketRecord>("SeasonSingleTickets");
        EnsureTable<SeasonPassRecord>("SeasonPasses");
        EnsureTable<MemberCardRecord>("MembershipCards");
        EnsureTable<EventVisitRecord>("TicketEventVisits");
        EnsureTable<EventVisitLogRecord>("TicketEventVisitsLogs");
        EnsureTable<EventFreeEntryRecord>("TicketEventFreeEntries");

        EnsureTable<FlexTicketBundleRecord>("FlexTicketBundles");

        EnsureTable<EventPriceRecord>("EventPrices");
        EnsureTable<EventPriceCategoryRecord>("EventPriceCategories");
        EnsureTable<SeasonPriceRecord>("SeasonPrices");
        EnsureTable<SeasonPriceCategoryRecord>("SeasonPriceCategories");

        return Task.CompletedTask;
    }

    private void EnsureTable<T>(string tableName) where T : class
    {
        if (!TableExists(tableName)) Create.Table<T>().Do();
    }
}

public class AddFlexTicketBundles(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists("FlexTicketBundles"))
            Create.Table<FlexTicketBundleRecord>().Do();

        if (!ColumnExists("SeasonSingleTickets", "BundleId"))
        {
            var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);
            if (isSqlite)
                Execute.Sql("ALTER TABLE SeasonSingleTickets ADD COLUMN BundleId INTEGER NULL").Do();
            else
                Alter.Table("SeasonSingleTickets").AddColumn("BundleId").AsInt32().Nullable().Do();
        }

        return Task.CompletedTask;
    }
}

public class AdjustMemberCardColumns(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);

        if (!ColumnExists("MembershipCards", "Reference"))
        {
            if (isSqlite)
                Execute.Sql("ALTER TABLE MembershipCards ADD COLUMN Reference NVARCHAR(100) NULL").Do();
            else
                Alter.Table("MembershipCards").AddColumn("Reference").AsString(100).Nullable().Do();
        }

        if (ColumnExists("MembershipCards", "Price"))
        {
            if (isSqlite)
                Execute.Sql("ALTER TABLE MembershipCards DROP COLUMN Price").Do();
            else
                Delete.Column("Price").FromTable("MembershipCards").Do();
        }

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

        keyValueService.SetValue(upgrader.StateValueKey, string.Empty);

        await upgrader.ExecuteAsync(migrationPlanExecutor, scopeProvider, keyValueService);
    }

    public Task TerminateAsync(bool isMainDom, CancellationToken cancellationToken) => Task.CompletedTask;
}
