namespace RedAnts.Features.Ticketing.Ports;

public interface IEventQuotasReader
{
    Task<IReadOnlyDictionary<int, int?>> GetAdmissionQuotasAsync();
    Task<IReadOnlyDictionary<int, int?>> GetSalesQuotasAsync();
}
