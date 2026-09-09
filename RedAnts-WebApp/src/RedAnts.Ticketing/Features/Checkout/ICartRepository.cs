using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Checkout;

public interface ICartRepository
{
    Cart Load();
    void Save(Cart cart);
    void Clear();
}
