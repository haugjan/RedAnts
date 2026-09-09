using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.Ports;

public interface IAdmissionRepository
{
    Task<Admission> LoadAsync(int eventId, TicketType ticketType, Guid ticketUuid);
    Task SaveAsync(Admission admission);
}

public interface IFreeEntryRepository
{
    Task<FreeEntryQuota> GetQuotaAsync(int eventId);
    Task SaveQuotaAsync(int eventId, FreeEntryQuota quota);
    Task<int> CountGrantedAsync(int eventId, FreeEntryType type);
    Task<FreeEntry?> FindLatestInsideAsync(int eventId, FreeEntryType type);
    Task SaveAsync(FreeEntry entry);
}

public interface IOccupancyReader
{
    Task<Occupancy> GetAsync(int eventId);
}

public sealed record AdmissionFacts(
    IssuedTicket? Issued,
    int? EventSeasonId,
    int? RedeemedEventId,
    bool IsBoxOfficeFlex,
    bool RequiresConversion,
    int? OriginType,
    string? OriginCardUuid);

public interface IAdmissionFactsReader
{
    Task<AdmissionFacts> ReadAsync(int eventId, TicketType ticketType, Guid ticketUuid);
}

public interface ITicketRedemptions
{
    Task MarkRedeemedAsync(TicketType ticketType, Guid ticketUuid, int eventId);
}
