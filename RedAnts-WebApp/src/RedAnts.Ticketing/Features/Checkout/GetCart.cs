using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Checkout;

public sealed record CartAddOnSummary(int Id, string Label, decimal Price, string SeasonName);

public sealed record CartLineSummary(
    string Key,
    string EventName,
    string CategoryName,
    string StandardCategoryName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    IReadOnlyList<CartAddOnSummary> AddOns,
    bool IsConversion,
    int OriginCap);

public sealed record CartSummary(
    IReadOnlyList<CartLineSummary> Items,
    IReadOnlyList<CartAddOnSummary> OrderAddOns,
    decimal TotalAmount,
    int TotalQuantity,
    bool IsEmpty,
    bool QualifiesForExpress,
    bool RequiresMobileNumber);

public static class GetCart
{
    public sealed record Query;

    public sealed class Handler(ICartRepository carts)
    {
        public Task<CartSummary> HandleAsync(Query query) => Task.FromResult(Summarize(carts.Load()));

        private static CartSummary Summarize(Cart cart) =>
            new(cart.Items.Select(ToLine).ToList(), cart.OrderAddOns.Select(ToAddOn).ToList(),
                cart.TotalAmount, cart.TotalQuantity, cart.IsEmpty, cart.QualifiesForExpress, cart.RequiresMobileNumber);

        private static CartLineSummary ToLine(CartLine line) =>
            new(line.Key, line.EventName, line.CategoryName, line.StandardCategoryName, line.UnitPrice, line.Quantity, line.LineTotal,
                line.AddOns.Select(ToAddOn).ToList(), line.IsConversion, line.OriginCap);

        private static CartAddOnSummary ToAddOn(CartAddOn addOn) => new(addOn.Id, addOn.Label, addOn.Price, addOn.SeasonName);
    }
}
