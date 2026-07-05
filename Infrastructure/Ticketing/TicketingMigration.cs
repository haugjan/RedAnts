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
        To<AddSeasonPriceTotalQuota>("seasonprices-total-quota");
        To<AddBuyerAndCreatorColumns>("buyer-creator-columns");
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

public class AddSeasonPriceTotalQuota(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("SeasonPrices", "TotalSalesQuota"))
        {
            var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);
            if (isSqlite)
                Execute.Sql("ALTER TABLE SeasonPrices ADD COLUMN TotalSalesQuota INTEGER NULL").Do();
            else
                Alter.Table("SeasonPrices").AddColumn("TotalSalesQuota").AsInt32().Nullable().Do();
        }

        return Task.CompletedTask;
    }
}

public class AddBuyerAndCreatorColumns(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        // Buyer (Privatperson/Firma) on the sold items and "created by" on every kind. All nullable,
        // so existing rows need no default. Idempotent: each column is added only when missing.
        AddInt("EventTickets", "BuyerType");
        AddString("EventTickets", "BuyerFirstName", 100);
        AddString("EventTickets", "BuyerLastName", 100);
        AddString("EventTickets", "BuyerCompany", 200);
        AddString("EventTickets", "CreatedByName", 200);

        AddInt("SeasonPasses", "BuyerType");
        AddString("SeasonPasses", "BuyerFirstName", 100);
        AddString("SeasonPasses", "BuyerLastName", 100);
        AddString("SeasonPasses", "BuyerCompany", 200);
        AddString("SeasonPasses", "CreatedByName", 200);

        AddInt("Orders", "BillingType");
        AddString("Orders", "BillingCompany", 200);

        AddString("MembershipCards", "CreatedByName", 200);
        AddString("FlexTicketBundles", "CreatedByName", 200);

        return Task.CompletedTask;
    }

    private bool IsSqlite =>
        Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);

    private void AddInt(string table, string column)
    {
        if (ColumnExists(table, column)) return;
        if (IsSqlite)
            Execute.Sql($"ALTER TABLE {table} ADD COLUMN {column} INTEGER NULL").Do();
        else
            Alter.Table(table).AddColumn(column).AsInt32().Nullable().Do();
    }

    private void AddString(string table, string column, int length)
    {
        if (ColumnExists(table, column)) return;
        if (IsSqlite)
            Execute.Sql($"ALTER TABLE {table} ADD COLUMN {column} NVARCHAR({length}) NULL").Do();
        else
            Alter.Table(table).AddColumn(column).AsString(length).Nullable().Do();
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
