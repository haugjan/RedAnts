using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

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
