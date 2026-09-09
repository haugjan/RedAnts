using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.Admission;

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
