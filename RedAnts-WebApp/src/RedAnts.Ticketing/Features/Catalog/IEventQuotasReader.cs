namespace RedAnts.Ticketing.Features.Catalog;

public interface IEventQuotasReader
{
    Task<IReadOnlyDictionary<int, int?>> GetAdmissionQuotasAsync();
    Task<IReadOnlyDictionary<int, int?>> GetSalesQuotasAsync();
}
