using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Ports;

public interface ICartRepository
{
    Cart Load();
    void Save(Cart cart);
    void Clear();
}

public interface IOrderTokens
{
    string Protect(int orderId);
    int? Unprotect(string? token);
}
