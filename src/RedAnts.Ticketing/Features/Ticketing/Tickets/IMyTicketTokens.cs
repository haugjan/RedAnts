namespace RedAnts.Features.Ticketing.Tickets;

public interface IMyTicketTokens
{
    string Create(string email);
    bool TryVerify(string token, out string email);
}
