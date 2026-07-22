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
        To<AddSeasonPriceDefaultTicketSalesQuota>("seasonprices-default-ticket-sales-quota");
        To<AddFreeEntryTypeQuotas>("freeentry-type-quotas");
        To<AddPriceTiers>("price-tiers");
        To<AddArticleGuids>("article-guids");
        To<AddSeasonAddOnInfoTexts>("seasonaddons-info-texts");
        To<AddOrderPaymentSource>("order-payment-source");
        To<AddSeasonAddOnTitleAndTiers>("seasonaddons-title-tiers");
        To<AddFreeEntryFixedCounts>("freeentry-fixed-counts");
        To<AddSeasonAddOnPromoOnly>("seasonaddons-promo-only");
        To<AddOrderNumberSequence>("order-number-sequence");
    }
}

public class AddOrderNumberSequence(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        var isSqlite = Database.DatabaseType.GetType().Name.Contains("SQLite", StringComparison.OrdinalIgnoreCase);
        if (isSqlite) return Task.CompletedTask;

        var exists = Database.ExecuteScalar<int>("SELECT COUNT(*) FROM sys.sequences WHERE name = 'OrderNumberSeq'");
        if (exists > 0) return Task.CompletedTask;

        var maxSuffix = Database.ExecuteScalar<int?>("SELECT MAX(TRY_CONVERT(int, RIGHT(OrderNumber, 6))) FROM Orders") ?? 0;
        var start = maxSuffix + 1;
        Execute.Sql($"CREATE SEQUENCE OrderNumberSeq AS bigint START WITH {start} INCREMENT BY 1").Do();

        return Task.CompletedTask;
    }
}

public class AddSeasonAddOnPromoOnly(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("SeasonAddOns", "PromoOnly"))
            Alter.Table("SeasonAddOns").AddColumn("PromoOnly").AsBoolean().NotNullable().WithDefaultValue(false).Do();
        return Task.CompletedTask;
    }
}

public class AddFreeEntryFixedCounts(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        AddInt("SuFixed");
        AddInt("PlayerFixed");
        AddInt("StaffFixed");
        AddInt("OfficialFixed");
        AddInt("ChildFixed");
        AddInt("HelperFixed");
        return Task.CompletedTask;
    }

    private void AddInt(string column)
    {
        if (ColumnExists("TicketEventFreeEntryQuotas", column)) return;
        Alter.Table("TicketEventFreeEntryQuotas").AddColumn(column).AsInt32().Nullable().Do();
    }
}

public class AddOrderPaymentSource(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("Orders", "PaymentSource"))
            Alter.Table("Orders").AddColumn("PaymentSource").AsInt32().Nullable().Do();
        return Task.CompletedTask;
    }
}

public class AddArticleGuids(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        AddGuid("EventPriceCategories");
        AddGuid("SeasonPriceCategories");
        AddGuid("SeasonAddOns");
        return Task.CompletedTask;
    }

    private void AddGuid(string table)
    {
        if (!ColumnExists(table, "ArticleGuid"))
            Alter.Table(table).AddColumn("ArticleGuid").AsGuid().Nullable().Do();
        Execute.Sql($"UPDATE {table} SET ArticleGuid = NEWID() WHERE ArticleGuid IS NULL").Do();
    }
}

public class AddSeasonAddOnInfoTexts(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        AddText("InfoBeforePurchase");
        AddText("InfoAfterPurchase");
        return Task.CompletedTask;

        void AddText(string column)
        {
            if (ColumnExists("SeasonAddOns", column)) return;
            Alter.Table("SeasonAddOns").AddColumn(column).AsString(2000).Nullable().Do();
        }
    }
}

public class AddSeasonAddOnTitleAndTiers(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        AddText("LongTitle");
        AddText("AllowedTierIds");
        return Task.CompletedTask;

        void AddText(string column)
        {
            if (ColumnExists("SeasonAddOns", column)) return;
            Alter.Table("SeasonAddOns").AddColumn(column).AsString(500).Nullable().Do();
        }
    }
}

public class AddSeasonPriceDefaultTicketSalesQuota(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("SeasonPrices", "DefaultTicketSalesQuota"))
            Alter.Table("SeasonPrices").AddColumn("DefaultTicketSalesQuota").AsInt32().Nullable().Do();
        return Task.CompletedTask;
    }
}

public class AddFreeEntryTypeQuotas(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        AddInt("PlayerQuota");
        AddInt("StaffQuota");
        AddInt("OfficialQuota");
        AddInt("ChildQuota");
        AddInt("HelperQuota");
        return Task.CompletedTask;
    }

    private void AddInt(string column)
    {
        if (ColumnExists("TicketEventFreeEntryQuotas", column)) return;
        Alter.Table("TicketEventFreeEntryQuotas").AddColumn(column).AsInt32().Nullable().Do();
    }
}

public class AddPriceTiers(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists("SeasonPriceTiers"))
            Create.Table<SeasonPriceTierRecord>().Do();
        else
            AddInt("SeasonPriceTiers", "LegacyCategory");

        AddInt("EventPriceCategories", "TierId");
        AddInt("SeasonPriceCategories", "TierId");
        AddInt("EventTickets", "TierId");
        AddInt("SeasonSingleTickets", "TierId");
        AddInt("SeasonPasses", "TierId");
        AddInt("OrderAddOns", "TierId");

        return Task.CompletedTask;
    }

    private void AddInt(string table, string column)
    {
        if (ColumnExists(table, column)) return;
        Alter.Table(table).AddColumn(column).AsInt32().Nullable().Do();
    }
}

public class AddOrderPaymentColumns(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("Orders", "PayrexxGatewayId"))
            Alter.Table("Orders").AddColumn("PayrexxGatewayId").AsString(100).Nullable().Do();

        if (!ColumnExists("Orders", "FulfillmentPayload"))
            Execute.Sql("ALTER TABLE Orders ADD FulfillmentPayload NVARCHAR(MAX) NULL").Do();

        return Task.CompletedTask;
    }
}

public class AddSeasonAddOnScope(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("SeasonAddOns", "Scope"))
            Alter.Table("SeasonAddOns").AddColumn("Scope").AsInt32().NotNullable().WithDefaultValue(0).Do();
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
            Alter.Table("EventTickets").AddColumn("BundleId").AsInt32().Nullable().Do();

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
        Alter.Table(table).AddColumn(column).AsDateTime().Nullable().Do();
    }
}

public class AddSeasonPassReference(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("SeasonPasses", "Reference"))
            Alter.Table("SeasonPasses").AddColumn("Reference").AsString(100).Nullable().Do();
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
        EnsureTable<OrderItemRecord>("OrderItems");

        EnsureTable<FlexTicketBundleRecord>("FlexTicketBundles");
        EnsureTable<EventTicketBundleRecord>("EventTicketBundles");

        EnsureTable<EventPriceRecord>("EventPrices");
        EnsureTable<EventPriceCategoryRecord>("EventPriceCategories");
        EnsureTable<SeasonPriceRecord>("SeasonPrices");
        EnsureTable<SeasonPriceTierRecord>("SeasonPriceTiers");
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
            Alter.Table("SeasonSingleTickets").AddColumn("BundleId").AsInt32().Nullable().Do();

        return Task.CompletedTask;
    }
}

public class AdjustMemberCardColumns(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("MembershipCards", "Reference"))
            Alter.Table("MembershipCards").AddColumn("Reference").AsString(100).Nullable().Do();

        if (ColumnExists("MembershipCards", "Price"))
            Delete.Column("Price").FromTable("MembershipCards").Do();

        return Task.CompletedTask;
    }
}

public class AddSeasonPriceTotalQuota(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("SeasonPrices", "TotalSalesQuota"))
            Alter.Table("SeasonPrices").AddColumn("TotalSalesQuota").AsInt32().Nullable().Do();

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

    private void AddInt(string table, string column)
    {
        if (ColumnExists(table, column)) return;
        Alter.Table(table).AddColumn(column).AsInt32().Nullable().Do();
    }

    private void AddString(string table, string column, int length)
    {
        if (ColumnExists(table, column)) return;
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
        Alter.Table(table).AddColumn("CreatedByEmail").AsString(200).Nullable().Do();
    }
}

public class AddFreeEntryUuid(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (ColumnExists("TicketEventVisits", "Uuid")) return Task.CompletedTask;
        Alter.Table("TicketEventVisits").AddColumn("Uuid").AsString(36).Nullable().Do();
        return Task.CompletedTask;
    }
}

public class AddSeasonPriceDualPricing(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("SeasonPriceCategories", "TicketPrice"))
            Alter.Table("SeasonPriceCategories").AddColumn("TicketPrice").AsDecimal(20, 2).Nullable().Do();

        if (!ColumnExists("SeasonPriceCategories", "Offered"))
            Alter.Table("SeasonPriceCategories").AddColumn("Offered").AsBoolean().Nullable().Do();

        return Task.CompletedTask;
    }
}

public class AddSeasonTicketDefaultColumns(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!ColumnExists("SeasonPriceCategories", "TicketOffered"))
        {
            Alter.Table("SeasonPriceCategories").AddColumn("TicketOffered").AsBoolean().Nullable().Do();
            Execute.Sql("UPDATE SeasonPriceCategories SET TicketOffered = Offered WHERE TicketOffered IS NULL").Do();
        }

        if (!ColumnExists("SeasonPriceCategories", "TicketQuota"))
            Alter.Table("SeasonPriceCategories").AddColumn("TicketQuota").AsInt32().Nullable().Do();

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
