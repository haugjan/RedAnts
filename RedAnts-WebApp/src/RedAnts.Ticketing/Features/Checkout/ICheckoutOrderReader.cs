namespace RedAnts.Ticketing.Features.Checkout;

public interface ICheckoutOrderReader
{
    Task<int?> FindIdByNumberAsync(string orderNumber);
}
