namespace RedAnts.Ticketing.Features.Checkout;

public interface IOrderTokens
{
    string Protect(int orderId);
    int? Unprotect(string? token);
}
