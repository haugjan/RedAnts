using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CheckoutWorkflow;

public static class ClearCart
{
    public sealed record Command;

    public sealed class Handler(ICartRepository carts)
    {
        public Task HandleAsync(Command command)
        {
            carts.Clear();
            return Task.CompletedTask;
        }
    }
}
