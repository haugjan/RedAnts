using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Catalog;

namespace RedAnts.Ticketing.Features.Checkout;

public sealed class CheckoutEligibility(IOccupancyReader occupancy, IEventConversionRuleRepository conversionRules)
{
    public async Task<CheckResult> EvaluateAsync(Cart cart, CheckoutSource source)
    {
        if (cart.IsEmpty) return cart.CheckoutBlocker(CheckoutContext.None);

        var fullEvents = new HashSet<int>();
        foreach (var eventId in cart.EventIds)
            if ((await occupancy.GetAsync(eventId)).Full)
                fullEvents.Add(eventId);

        var conversionOnlyEvents = new HashSet<int>();
        foreach (var eventId in cart.EventIds.Where(cart.HasRegularTicketsFor))
            if (await conversionRules.GetConversionOnlyAsync(eventId))
                conversionOnlyEvents.Add(eventId);

        return cart.CheckoutBlocker(new CheckoutContext(fullEvents, conversionOnlyEvents, source == CheckoutSource.Express));
    }
}
