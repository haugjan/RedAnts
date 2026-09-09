
namespace RedAnts.Ticketing.Features.FlexTickets;

public static class DeleteEmptyFlexBundle
{
    public sealed record Command(int BundleId);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task<bool> HandleAsync(Command command) => bundles.DeleteEmptyAsync(command.BundleId);
    }
}
