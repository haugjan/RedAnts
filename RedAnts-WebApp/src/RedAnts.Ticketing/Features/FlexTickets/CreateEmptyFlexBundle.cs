using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.FlexTickets;

public static class CreateEmptyFlexBundle
{
    public sealed record Command(int SeasonId, TicketCategory Category, string Reference, string? CreatedByName, string? CreatedByEmail);

    public sealed class Handler(IFlexTicketBundleRepository bundles)
    {
        public async Task<int> HandleAsync(Command command)
        {
            var reference = FlexBundleReference.Clean(command.Reference);
            await FlexBundleReference.RequireFreeAsync(bundles, command.SeasonId, reference);
            return await bundles.CreateEmptyAsync(command.SeasonId, command.Category, reference, command.CreatedByName, command.CreatedByEmail);
        }
    }
}
