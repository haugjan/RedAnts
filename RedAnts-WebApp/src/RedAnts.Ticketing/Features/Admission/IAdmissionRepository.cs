using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admission;

public interface IAdmissionRepository
{
    Task<Domain.Admission.Admission> LoadAsync(int eventId, TicketType ticketType, Guid ticketUuid);
    Task SaveAsync(Domain.Admission.Admission admission);
}
