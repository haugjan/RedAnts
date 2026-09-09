namespace RedAnts.Ticketing.Features.Tickets;

public interface IAdminTicketDeletion
{
    Task DeleteEventTicketAsync(Guid uuid);
    Task DeleteFlexTicketAsync(Guid uuid);
    Task DeleteSeasonPassAsync(Guid uuid);
    Task DeleteMemberCardAsync(Guid uuid);
}
