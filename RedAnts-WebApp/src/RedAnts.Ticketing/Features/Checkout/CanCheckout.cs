namespace RedAnts.Ticketing.Features.Checkout;

public static class CanCheckout
{
    public sealed record Check(CheckoutSource Source = CheckoutSource.Checkout);

    public sealed class Handler(ICartRepository carts, CheckoutEligibility eligibility)
    {
        public Task<CheckResult> HandleAsync(Check check) => eligibility.EvaluateAsync(carts.Load(), check.Source);
    }
}
