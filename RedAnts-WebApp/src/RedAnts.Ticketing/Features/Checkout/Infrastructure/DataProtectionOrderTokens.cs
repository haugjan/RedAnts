using Microsoft.AspNetCore.DataProtection;

namespace RedAnts.Ticketing.Features.Checkout.Infrastructure;

public sealed class DataProtectionOrderTokens(IDataProtectionProvider dataProtection) : IOrderTokens
{
    private readonly IDataProtector _protector = dataProtection.CreateProtector("RedAnts.CheckoutOrder.v1");

    public string Protect(int orderId) => _protector.Protect(orderId.ToString());

    public int? Unprotect(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        try
        {
            return int.TryParse(_protector.Unprotect(token), out var orderId) ? orderId : null;
        }
        catch
        {
            return null;
        }
    }
}
