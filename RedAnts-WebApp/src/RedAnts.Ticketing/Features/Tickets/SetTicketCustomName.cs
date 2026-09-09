using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public static class SetTicketCustomName
{
    public sealed record Command(string Token, string? CustomName);

    public sealed class Handler(WebTicketResolution resolution, ITicketCustomNames customNames)
    {
        public async Task<bool> HandleAsync(Command command)
        {
            var resolved = await resolution.ResolveAsync(command.Token);
            if (resolved is not { Issued.Status: TicketStatus.Valid }) return false;

            await customNames.SetAsync(resolved.Data.Type, resolved.Data.Uuid, command.CustomName);
            return true;
        }
    }
}
