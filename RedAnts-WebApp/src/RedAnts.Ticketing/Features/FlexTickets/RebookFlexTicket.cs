
namespace RedAnts.Ticketing.Features.FlexTickets;

public static class RebookFlexTicket
{
    public sealed record Command(int TargetBundleId, Guid? Uuid, string? Code, string? OperatorName);

    public sealed class Handler(IFlexTicketBundleRepository bundles)
    {
        public Task<FlexRebookResult> HandleAsync(Command command) =>
            command.Uuid is { } uuid
                ? bundles.RebookByUuidAsync(command.TargetBundleId, uuid, command.OperatorName)
                : bundles.RebookByCodeAsync(command.TargetBundleId, command.Code ?? "", command.OperatorName);
    }
}
