using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Checkout;

public static class RemoveOrderAddOnFromCart
{
    public sealed record Command(int AddOnId);

    public sealed class Handler(ICartRepository carts)
    {
        public Task<Cart> HandleAsync(Command command)
        {
            var cart = carts.Load();
            cart.RemoveOrderAddOn(command.AddOnId);
            carts.Save(cart);
            return Task.FromResult(cart);
        }
    }
}
