namespace RedAnts.Ticketing.Features.Orders;

public interface IDraftOrdersReader
{
    Task<IReadOnlyList<int>> GetIdsCreatedBetweenAsync(DateTimeOffset createdAfter, DateTimeOffset createdBefore);
}
