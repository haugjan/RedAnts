using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Ports;

public interface ISingleTickets
{
    Task<IReadOnlyList<SingleTicket>> GetAllAsync();
    Task<IReadOnlyList<SingleTicket>> GetByEventAsync(int eventId);
    Task<SingleTicket?> FindByIdAsync(int id);
    Task<SingleTicket?> FindByTicketGuidAsync(Guid ticketId);
    Task<SingleTicket> SaveAsync(SingleTicket ticket);
    Task UpdateAsync(SingleTicket ticket);
}

public interface ISeasonTickets
{
    Task<IReadOnlyList<SeasonTicket>> GetAllAsync();
    Task<IReadOnlyList<SeasonTicket>> GetBySeasonAsync(int seasonId);
    Task<SeasonTicket?> FindByIdAsync(int id);
    Task<SeasonTicket?> FindByTicketGuidAsync(Guid seasonTicketId);
    Task<SeasonTicket> SaveAsync(SeasonTicket ticket);
    Task UpdateAsync(SeasonTicket ticket);
}

public interface IMemberCards
{
    Task<IReadOnlyList<MemberCard>> GetAllAsync();
    Task<IReadOnlyList<MemberCard>> GetBySeasonAsync(int seasonId);
    Task<MemberCard?> FindByIdAsync(int id);
    Task<MemberCard> SaveAsync(MemberCard card);
    Task UpdateAsync(MemberCard card);
    Task DeleteAsync(int id);
}

public sealed record TicketScanEntry(TicketKind Kind, Guid TicketRef, ScanDirection Direction, DateTime ScannedAt, string ScannedBy);

public interface ITicketScanLog
{
    Task AppendAsync(TicketScanEntry entry);
    Task<IReadOnlyList<TicketScanEntry>> GetForTicketAsync(Guid ticketRef);
}
