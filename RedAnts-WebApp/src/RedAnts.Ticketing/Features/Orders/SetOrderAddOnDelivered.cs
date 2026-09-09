namespace RedAnts.Ticketing.Features.Orders;

public static class SetOrderAddOnDelivered
{
    public sealed record Command(int OrderAddOnId, bool Delivered);

    public sealed class Handler(IOrderAddOns addOns)
    {
        public Task HandleAsync(Command command) => addOns.SetDeliveredAsync(command.OrderAddOnId, command.Delivered);
    }
}
