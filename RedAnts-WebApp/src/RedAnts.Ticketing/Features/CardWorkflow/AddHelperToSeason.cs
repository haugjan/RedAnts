using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class AddHelperToSeason
{
    public sealed record Command(int SeasonId, string FirstName, string LastName, string Email);

    public sealed class Handler(IHelpers helpers)
    {
        public Task<Helper> HandleAsync(Command command) =>
            helpers.AddAsync(command.SeasonId, command.FirstName, command.LastName, command.Email);
    }
}
