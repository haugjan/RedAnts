namespace RedAnts.Features.Ticketing.Admin;

public interface IAdminTicketDeletion
{
    Task DeleteEventTicketAsync(Guid uuid);
    Task DeleteFlexTicketAsync(Guid uuid);
    Task DeleteSeasonPassAsync(Guid uuid);
    Task DeleteMemberCardAsync(Guid uuid);
}
