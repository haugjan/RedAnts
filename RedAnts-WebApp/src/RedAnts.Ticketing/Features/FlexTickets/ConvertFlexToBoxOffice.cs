
namespace RedAnts.Ticketing.Features.FlexTickets;

public static class ConvertFlexToBoxOffice
{
    public sealed record Command(Guid? Uuid, string? Code, string? OperatorName);

    public sealed class Handler(IFlexTicketBundleRepository bundles)
    {
        public Task<FlexBoxOfficeResult> HandleAsync(Command command) =>
            command.Uuid is { } uuid
                ? bundles.ConvertToBoxOfficeByUuidAsync(uuid, command.OperatorName)
                : bundles.ConvertToBoxOfficeByCodeAsync(command.Code ?? "", command.OperatorName);
    }
}
