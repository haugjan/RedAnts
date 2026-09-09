using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Orders;
using Xunit;

namespace RedAnts.Ticketing.Tests.Orders;

public class GetOrdersForAdminTests
{
    private readonly StubOrderListReader _reader = new();
    private readonly StubEvents _events = new();

    private GetOrdersForAdmin.Handler Handler => new(_reader, _events);

    public GetOrdersForAdminTests()
    {
        _events.Events.Add(StubEvents.InSeason(10, 3));
        _events.Events.Add(StubEvents.InSeason(11, 3));
        _events.Events.Add(StubEvents.InSeason(20, 4));
        _reader.Rows.Add(StubOrderListReader.Row(1, "AGT-1", "Anna Muster", "anna@example.ch"));
        _reader.Rows.Add(StubOrderListReader.Row(2, "AGT-2", "Beat Beispiel", "beat@example.ch", OrderStatus.Draft, "Zürich"));
    }

    [Fact]
    public async Task Passes_the_season_and_its_event_ids_to_the_reader_and_returns_every_row()
    {
        var result = await Handler.HandleAsync(new GetOrdersForAdmin.Query(3));

        Assert.Equal(3, _reader.LastCall!.Value.SeasonId);
        Assert.Equal([10, 11], _reader.LastCall.Value.EventIds);
        Assert.Equal(2, result.Total);
        Assert.Equal(["AGT-1", "AGT-2"], result.Orders.Select(o => o.OrderNumber));
    }

    [Theory]
    [InlineData("beat", "AGT-2")]
    [InlineData("zürich", "AGT-2")]
    [InlineData("AGT-1", "AGT-1")]
    [InlineData("unbezahlt", "AGT-2")]
    [InlineData("Anna example.ch", "AGT-1")]
    public async Task Filters_by_every_search_term_across_number_name_address_and_status(string search, string expected)
    {
        var result = await Handler.HandleAsync(new GetOrdersForAdmin.Query(3, search));

        Assert.Equal(2, result.Total);
        Assert.Equal(expected, Assert.Single(result.Orders).OrderNumber);
    }

    [Fact]
    public async Task A_missing_season_yields_nothing_without_reading()
    {
        var result = await Handler.HandleAsync(new GetOrdersForAdmin.Query(0));

        Assert.Same(OrdersForAdmin.Empty, result);
        Assert.Null(_reader.LastCall);
    }
}
