using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Helpers;

public static class AddHelperToSeason
{
    public sealed record Command(int SeasonId, string FirstName, string LastName, string Email);

    public sealed class Handler(IHelperRepository helpers)
    {
        public Task<Helper> HandleAsync(Command command) =>
            helpers.AddAsync(command.SeasonId, command.FirstName, command.LastName, command.Email);
    }
}
