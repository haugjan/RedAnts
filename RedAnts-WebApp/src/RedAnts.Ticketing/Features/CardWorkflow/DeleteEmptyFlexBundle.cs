using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class DeleteEmptyFlexBundle
{
    public sealed record Command(int BundleId);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task<bool> HandleAsync(Command command) => bundles.DeleteEmptyAsync(command.BundleId);
    }
}
