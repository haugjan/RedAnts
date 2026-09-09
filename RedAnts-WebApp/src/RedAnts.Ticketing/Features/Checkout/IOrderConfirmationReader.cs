namespace RedAnts.Ticketing.Features.Checkout;

public sealed record ConfirmedEventTicket(Guid Uuid, int EventId, int? TierId);

public sealed record ConfirmedSeasonPass(Guid Uuid, int SeasonId, int? TierId);

public interface IOrderConfirmationReader
{
    Task<IReadOnlyList<ConfirmedEventTicket>> GetEventTicketsAsync(int orderId);

    Task<IReadOnlyList<ConfirmedSeasonPass>> GetSeasonPassesAsync(int orderId);
}
