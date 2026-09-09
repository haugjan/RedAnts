namespace RedAnts.Ticketing.Features.Catalog;

public interface ITierSalesReader
{
    Task<int> GetSoldCountAsync(int tierId);
}
