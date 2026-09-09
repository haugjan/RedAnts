using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CheckoutWorkflow;

internal static class AvailableTicketCategoryNames
{
    public static string StandardName(this AvailableTicketCategory category) =>
        string.IsNullOrWhiteSpace(category.ShortName) ? category.Name : category.ShortName!;
}
