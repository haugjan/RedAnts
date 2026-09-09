namespace RedAnts.Ticketing.Features.Tickets;

public interface IPublicBaseUrl
{
    string Resolve();

    string TicketUrl(string token);
}
