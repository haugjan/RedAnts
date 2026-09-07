using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class ConvertFlexToBoxOffice
{
    public sealed record Command(Guid? Uuid, string? Code, string? OperatorName);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task<FlexBoxOfficeResult> HandleAsync(Command command) =>
            command.Uuid is { } uuid
                ? bundles.ConvertToBoxOfficeByUuidAsync(uuid, command.OperatorName)
                : bundles.ConvertToBoxOfficeByCodeAsync(command.Code ?? "", command.OperatorName);
    }
}
