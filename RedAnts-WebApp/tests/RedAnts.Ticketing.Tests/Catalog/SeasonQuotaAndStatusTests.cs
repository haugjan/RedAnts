using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Catalog.Pricing;
using Xunit;

namespace RedAnts.Ticketing.Tests.Catalog;

public class SeasonQuotaAndStatusTests
{
    private const int SeasonId = 3;

    private static SeasonPrice ExistingPrice() => SeasonPrice.FromPersistence(9, SeasonId, 400,
        [SeasonCategoryPrice.FromPersistence(TicketCategory.Adult, 300m, true, 50, 20m, true, null, tierId: 7)],
        defaultTicketSalesQuota: 120, reserved: 4, version: 2);

    [Fact]
    public async Task Pass_quota_creates_a_price_row_when_none_exists()
    {
        var prices = new InMemorySeasonPrices();

        await new SetSeasonPassQuota.Handler(prices).HandleAsync(new SetSeasonPassQuota.Command(SeasonId, 250));

        var stored = prices.Stored[SeasonId];
        Assert.Equal(250, stored.TotalSalesQuota);
        Assert.Null(stored.DefaultTicketSalesQuota);
        Assert.Empty(stored.Categories);
        Assert.NotEqual(0, stored.Id);
    }

    [Fact]
    public async Task Pass_quota_keeps_categories_ticket_quota_and_reservation_of_an_existing_row()
    {
        var prices = new InMemorySeasonPrices();
        prices.Seed(ExistingPrice());

        await new SetSeasonPassQuota.Handler(prices).HandleAsync(new SetSeasonPassQuota.Command(SeasonId, null));

        var stored = prices.Stored[SeasonId];
        Assert.Null(stored.TotalSalesQuota);
        Assert.Equal(120, stored.DefaultTicketSalesQuota);
        Assert.Single(stored.Categories);
        Assert.Equal(4, stored.Reserved);
        Assert.Equal(2, stored.Version);
        Assert.Equal(9, stored.Id);
    }

    [Fact]
    public async Task Ticket_sales_quota_creates_a_price_row_when_none_exists()
    {
        var prices = new InMemorySeasonPrices();

        await new SetSeasonTicketSalesQuota.Handler(prices).HandleAsync(new SetSeasonTicketSalesQuota.Command(SeasonId, 80));

        var stored = prices.Stored[SeasonId];
        Assert.Equal(80, stored.DefaultTicketSalesQuota);
        Assert.Null(stored.TotalSalesQuota);
    }

    [Fact]
    public async Task Ticket_sales_quota_keeps_the_pass_quota_and_categories_of_an_existing_row()
    {
        var prices = new InMemorySeasonPrices();
        prices.Seed(ExistingPrice());

        await new SetSeasonTicketSalesQuota.Handler(prices).HandleAsync(new SetSeasonTicketSalesQuota.Command(SeasonId, 60));

        var stored = prices.Stored[SeasonId];
        Assert.Equal(60, stored.DefaultTicketSalesQuota);
        Assert.Equal(400, stored.TotalSalesQuota);
        Assert.Single(stored.Categories);
    }

    [Fact]
    public async Task Negative_quotas_are_rejected_without_saving()
    {
        var prices = new InMemorySeasonPrices();
        prices.Seed(ExistingPrice());

        await Assert.ThrowsAsync<DomainException>(() =>
            new SetSeasonPassQuota.Handler(prices).HandleAsync(new SetSeasonPassQuota.Command(SeasonId, -1)));
        await Assert.ThrowsAsync<DomainException>(() =>
            new SetSeasonTicketSalesQuota.Handler(prices).HandleAsync(new SetSeasonTicketSalesQuota.Command(SeasonId, -5)));

        Assert.Equal(0, prices.SaveCalls);
    }

    [Fact]
    public async Task Sales_status_is_forwarded_to_the_content_status()
    {
        var status = new RecordingSeasonStatus();

        await new SetSeasonSalesStatus.Handler(status).HandleAsync(new SetSeasonSalesStatus.Command(SeasonId, SeasonStatus.Intern));

        Assert.Equal([(SeasonId, SeasonStatus.Intern)], status.Calls);
    }
}
