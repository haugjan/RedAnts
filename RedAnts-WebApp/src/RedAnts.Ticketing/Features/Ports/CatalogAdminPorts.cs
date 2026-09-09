namespace RedAnts.Ticketing.Features.Ports;

public interface IEventQuotasReader
{
    Task<IReadOnlyDictionary<int, int?>> GetAdmissionQuotasAsync();
    Task<IReadOnlyDictionary<int, int?>> GetSalesQuotasAsync();
}
