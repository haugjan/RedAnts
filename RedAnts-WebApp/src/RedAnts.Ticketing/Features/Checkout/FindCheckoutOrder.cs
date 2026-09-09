namespace RedAnts.Ticketing.Features.Checkout;

public static class FindCheckoutOrder
{
    public sealed record Query(string? Token = null, string? OrderNumber = null);

    public sealed class Handler(IOrderTokens tokens, ICheckoutOrderReader orders)
    {
        public async Task<int?> HandleAsync(Query query)
        {
            if (!string.IsNullOrWhiteSpace(query.Token)) return tokens.Unprotect(query.Token);
            if (!string.IsNullOrWhiteSpace(query.OrderNumber)) return await orders.FindIdByNumberAsync(query.OrderNumber.Trim());
            return null;
        }
    }
}
