using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CheckoutWorkflow;

public static class ChangeCartLineQuantity
{
    public sealed record Command(string Key, int Quantity);

    public sealed class Handler(ICartRepository carts)
    {
        public Task<Cart> HandleAsync(Command command)
        {
            var cart = carts.Load();
            cart.SetQuantity(command.Key, command.Quantity);
            carts.Save(cart);
            return Task.FromResult(cart);
        }
    }
}
