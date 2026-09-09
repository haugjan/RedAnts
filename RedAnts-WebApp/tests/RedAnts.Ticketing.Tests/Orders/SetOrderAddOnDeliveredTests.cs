using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Tests.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Orders;

public class SetOrderAddOnDeliveredTests
{
    [Fact]
    public async Task Marks_the_add_on_delivered_or_open()
    {
        var addOns = new RecordingOrderAddOns();
        var handler = new SetOrderAddOnDelivered.Handler(addOns);

        await handler.HandleAsync(new SetOrderAddOnDelivered.Command(5, true));
        await handler.HandleAsync(new SetOrderAddOnDelivered.Command(5, false));

        Assert.Equal([(5, true), (5, false)], addOns.Deliveries);
    }
}
