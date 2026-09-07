using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CheckoutWorkflow;

internal static class AvailableTicketCategoryNames
{
    public static string StandardName(this AvailableTicketCategory category) =>
        string.IsNullOrWhiteSpace(category.ShortName) ? category.Name : category.ShortName!;
}
