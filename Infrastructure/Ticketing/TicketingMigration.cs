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
        To<AddCreatedByEmailColumns>("createdby-email-columns");
        To<AddFreeEntryUuid>("freeentry-uuid");
        To<AddSeasonPriceDualPricing>("seasonprices-dual-price");
        To<AddSeasonTicketDefaultColumns>("seasonprices-ticket-defaults");
        To<AddSeasonPassReference>("seasonpasses-reference");
        To<AddCategoryTimeWindows>("category-time-windows");
        To<AddEventTicketBundles>("eventticket-bundles");
        To<AddSalesFilterIndexes>("sales-filter-indexes");
        To<AddOrderPaymentColumns>("order-payment-columns");
        To<AddSeasonAddOnScope>("seasonaddons-scope");
    }
}

public class AddOrderPaymentColumns(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);

        if (!ColumnExists("Orders", "PayrexxGatewayId"))
        {
            if (isSqlite)
                Execute.Sql("ALTER TABLE Orders ADD COLUMN PayrexxGatewayId NVARCHAR(100) NULL").Do();
            else
                Alter.Table("Orders").AddColumn("PayrexxGatewayId").AsString(100).Nullable().Do();
        }

        if (!ColumnExists("Orders", "FulfillmentPayload"))
        {
            if (isSqlite)
                Execute.Sql("ALTER TABLE Orders ADD COLUMN FulfillmentPayload TEXT NULL").Do();
            else
                Execute.Sql("ALTER TABLE Orders ADD FulfillmentPayload NVARCHAR(MAX) NULL").Do();
        }

        return Task.CompletedTask;
    }
}

public class AddSeasonAddOnScope(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);

        if (!ColumnExists("SeasonAddOns", "Scope"))
        {
            if (isSqlite)
                Execute.Sql("ALTER TABLE SeasonAddOns ADD COLUMN Scope INT NOT NULL DEFAULT 0").Do();
            else
                Alter.Table("SeasonAddOns").AddColumn("Scope").AsInt32().NotNullable().WithDefaultValue(0).Do();
        }

        return Task.CompletedTask;
    }
}

public class AddSalesFilterIndexes(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        EnsureIndex("EventTickets", "IX_EventTickets_EventId", "EventId");
        EnsureIndex("EventTickets", "IX_EventTickets_BundleId", "BundleId");
        EnsureIndex("SeasonSingleTickets", "IX_SeasonSingleTickets_SeasonId", "SeasonId");
        EnsureIndex("SeasonPasses", "IX_SeasonPasses_SeasonId", "SeasonId");
        EnsureIndex("MembershipCards", "IX_MembershipCards_SeasonId", "SeasonId");
        return Task.CompletedTask;
    }

    private void EnsureIndex(string table, string indexName, string columns)
    {
        var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);
        if (isSqlite)
        {
            Execute.Sql($"CREATE INDEX IF NOT EXISTS {indexName} ON {table} ({columns})").Do();
            return;
        }
        Execute.Sql(
            $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID('{table}')) " +
            $"CREATE NONCLUSTERED INDEX {indexName} ON {table} ({columns})").Do();
    }
}

public class AddEventTicketBundles(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists("EventTicketBundles"))
            Create.Table<EventTicketBundleRecord>().Do();

        if (!ColumnExists("EventTickets", "BundleId"))
        {
            var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);
            if (isSqlite)
                Execute.Sql("ALTER TABLE EventTickets ADD COLUMN BundleId INTEGER NULL").Do();
            else
                Alter.Table("EventTickets").AddColumn("BundleId").AsInt32().Nullable().Do();
        }

        return Task.CompletedTask;
    }
}

public class AddCategoryTimeWindows(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        AddDate("EventPriceCategories", "AvailableUntil");
        AddDate("SeasonPriceCategories", "PassAvailableUntil");
        AddDate("SeasonPriceCategories", "TicketAvailableUntil");
        return Task.CompletedTask;
    }

    private void AddDate(string table, string column)
    {
        if (ColumnExists(table, column)) return;
        var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);
        if (isSqlite)
            Execute.Sql($"ALTER TABLE {table} ADD COLUMN {column} DATETIME NULL").Do();
        else
            Alter.Table(table).AddColumn(column).AsDateTime().Nullable().Do();
    }
}

public class AddSeasonPassReference(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("SeasonPasses", "Reference"))
        {
            var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);
            if (isSqlite)
                Execute.Sql("ALTER TABLE SeasonPasses ADD COLUMN Reference NVARCHAR(100) NULL").Do();
            else
                Alter.Table("SeasonPasses").AddColumn("Reference").AsString(100).Nullable().Do();
        }
        return Task.CompletedTask;
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
        EnsureTable<EventFreeEntryQuotaRecord>("TicketEventFreeEntryQuotas");
        EnsureTable<OrderStatusLogRecord>("OrderStatusLogs");
        EnsureTable<NewsletterSignupRecord>("NewsletterSignups");
        EnsureTable<OrderAddOnRecord>("OrderAddOns");

        EnsureTable<FlexTicketBundleRecord>("FlexTicketBundles");
        EnsureTable<EventTicketBundleRecord>("EventTicketBundles");

        EnsureTable<EventPriceRecord>("EventPrices");
        EnsureTable<EventPriceCategoryRecord>("EventPriceCategories");
        EnsureTable<SeasonPriceRecord>("SeasonPrices");
        EnsureTable<SeasonPriceCategoryRecord>("SeasonPriceCategories");
        EnsureTable<SeasonAddOnRecord>("SeasonAddOns");

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

public class AddCreatedByEmailColumns(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        AddEmail("EventTickets");
        AddEmail("SeasonPasses");
        AddEmail("MembershipCards");
        AddEmail("FlexTicketBundles");
        return Task.CompletedTask;
    }

    private void AddEmail(string table)
    {
        if (ColumnExists(table, "CreatedByEmail")) return;
        var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);
        if (isSqlite)
            Execute.Sql($"ALTER TABLE {table} ADD COLUMN CreatedByEmail NVARCHAR(200) NULL").Do();
        else
            Alter.Table(table).AddColumn("CreatedByEmail").AsString(200).Nullable().Do();
    }
}

public class AddFreeEntryUuid(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (ColumnExists("TicketEventVisits", "Uuid")) return Task.CompletedTask;
        var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);
        if (isSqlite)
            Execute.Sql("ALTER TABLE TicketEventVisits ADD COLUMN Uuid NVARCHAR(36) NULL").Do();
        else
            Alter.Table("TicketEventVisits").AddColumn("Uuid").AsString(36).Nullable().Do();
        return Task.CompletedTask;
    }
}

public class AddSeasonPriceDualPricing(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);

        if (!ColumnExists("SeasonPriceCategories", "TicketPrice"))
        {
            if (isSqlite)
                Execute.Sql("ALTER TABLE SeasonPriceCategories ADD COLUMN TicketPrice DECIMAL(20,2) NULL").Do();
            else
                Alter.Table("SeasonPriceCategories").AddColumn("TicketPrice").AsDecimal(20, 2).Nullable().Do();
        }

        if (!ColumnExists("SeasonPriceCategories", "Offered"))
        {
            if (isSqlite)
                Execute.Sql("ALTER TABLE SeasonPriceCategories ADD COLUMN Offered INTEGER NULL").Do();
            else
                Alter.Table("SeasonPriceCategories").AddColumn("Offered").AsBoolean().Nullable().Do();
        }

        return Task.CompletedTask;
    }
}

public class AddSeasonTicketDefaultColumns(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);

        if (!ColumnExists("SeasonPriceCategories", "TicketOffered"))
        {
            if (isSqlite)
                Execute.Sql("ALTER TABLE SeasonPriceCategories ADD COLUMN TicketOffered INTEGER NULL").Do();
            else
                Alter.Table("SeasonPriceCategories").AddColumn("TicketOffered").AsBoolean().Nullable().Do();
            Execute.Sql("UPDATE SeasonPriceCategories SET TicketOffered = Offered WHERE TicketOffered IS NULL").Do();
        }

        if (!ColumnExists("SeasonPriceCategories", "TicketQuota"))
        {
            if (isSqlite)
                Execute.Sql("ALTER TABLE SeasonPriceCategories ADD COLUMN TicketQuota INTEGER NULL").Do();
            else
                Alter.Table("SeasonPriceCategories").AddColumn("TicketQuota").AsInt32().Nullable().Do();
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
