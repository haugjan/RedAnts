namespace RedAnts.Domain.Ticketing;

/// <summary>
/// One resolved sales price row from the "salesPrices" Block List on an event or season.
/// The category is a reference to a central <c>ticketCategory</c> content node (stable
/// <see cref="CategoryCode"/> + display <see cref="CategoryName"/>). <see cref="Price"/> is the
/// effective price: the category's default price when the row uses it, otherwise the row's own price.
/// <see cref="Contingent"/> is the sales quota (captured only for now; no sell-out logic yet).
/// </summary>
public sealed record TicketPrice(string CategoryCode, string CategoryName, decimal Price, int? Contingent)
{
    public bool HasContingent => Contingent is > 0;
}
