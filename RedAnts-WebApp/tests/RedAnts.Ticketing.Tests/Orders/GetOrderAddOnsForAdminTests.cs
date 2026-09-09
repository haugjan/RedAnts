using RedAnts.Ticketing.Features.Orders;
using Xunit;

namespace RedAnts.Ticketing.Tests.Orders;

public class GetOrderAddOnsForAdminTests
{
    private readonly StubOrderAddOnListReader _reader = new();

    private GetOrderAddOnsForAdmin.Handler Handler => new(_reader);

    public GetOrderAddOnsForAdminTests()
    {
        _reader.Rows.Add(StubOrderAddOnListReader.Row(1, "AGT-1", "Anna", "Parkplatz", 1, delivered: true));
        _reader.Rows.Add(StubOrderAddOnListReader.Row(2, "AGT-2", "Beat", "Trikot", 3, delivered: false));
        _reader.Rows.Add(StubOrderAddOnListReader.Row(3, "AGT-3", "Cleo", "Parkplatz", 2, delivered: false));
    }

    [Fact]
    public async Task Returns_all_rows_with_the_season_totals()
    {
        var result = await Handler.HandleAsync(new GetOrderAddOnsForAdmin.Query(3));

        Assert.Equal(3, _reader.LastSeasonId);
        Assert.Equal((3, 1, 6), (result.Total, result.DeliveredCount, result.TotalQuantity));
        Assert.Equal(3, result.AddOns.Count);
    }

    [Fact]
    public async Task Only_open_hides_delivered_rows_but_keeps_the_totals()
    {
        var result = await Handler.HandleAsync(new GetOrderAddOnsForAdmin.Query(3, OnlyOpen: true));

        Assert.Equal(["AGT-2", "AGT-3"], result.AddOns.Select(a => a.OrderNumber));
        Assert.Equal((3, 1, 6), (result.Total, result.DeliveredCount, result.TotalQuantity));
    }

    [Fact]
    public async Task Search_matches_buyer_label_and_number()
    {
        var byLabel = await Handler.HandleAsync(new GetOrderAddOnsForAdmin.Query(3, "parkplatz"));
        var byBuyerAndOpen = await Handler.HandleAsync(new GetOrderAddOnsForAdmin.Query(3, "cleo", OnlyOpen: true));

        Assert.Equal(["AGT-1", "AGT-3"], byLabel.AddOns.Select(a => a.OrderNumber));
        Assert.Equal("AGT-3", Assert.Single(byBuyerAndOpen.AddOns).OrderNumber);
    }

    [Fact]
    public async Task A_missing_season_yields_nothing_without_reading()
    {
        var result = await Handler.HandleAsync(new GetOrderAddOnsForAdmin.Query(0));

        Assert.Same(OrderAddOnsForAdmin.Empty, result);
        Assert.Null(_reader.LastSeasonId);
    }
}
