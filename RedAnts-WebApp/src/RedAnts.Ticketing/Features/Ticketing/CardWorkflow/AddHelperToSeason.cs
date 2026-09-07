using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class AddHelperToSeason
{
    public sealed record Command(int SeasonId, string FirstName, string LastName, string Email);

    public sealed class Handler(IHelpers helpers)
    {
        public Task<Helper> HandleAsync(Command command) =>
            helpers.AddAsync(command.SeasonId, command.FirstName, command.LastName, command.Email);
    }
}
