using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.AdmissionWorkflow;

namespace RedAnts.Features.Ticketing.Scanning;

public interface IAdmissionService
{
    Task<Occupancy> GetOccupancyAsync(int eventId);

    Task<ScanOutcome> ScanTicketAsync(int eventId, TicketType type, Guid uuid, int scopeId, ScanMode mode, string? scannedBy, bool test = false);

    Task<ScanOutcome> ScanCodeAsync(int eventId, string shortCode, ScanMode mode, string? scannedBy, bool test = false);

    Task<ScanOutcome> GrantFreeEntryAsync(int eventId, FreeEntryType type, string? scannedBy);

    Task<ScanOutcome> RevokeFreeEntryAsync(int eventId, FreeEntryType type, string? scannedBy);
}
