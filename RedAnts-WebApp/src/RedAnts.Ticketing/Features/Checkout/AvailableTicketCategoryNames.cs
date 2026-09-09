
namespace RedAnts.Ticketing.Features.Checkout;

internal static class AvailableTicketCategoryNames
{
    public static string StandardName(this AvailableTicketCategory category) =>
        string.IsNullOrWhiteSpace(category.ShortName) ? category.Name : category.ShortName!;
}
