
namespace RedAnts.Ticketing.Features.FlexTickets;

public static class SetFlexTicketRedeemed
{
    public sealed record Command(Guid Uuid, bool Redeemed);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task HandleAsync(Command command) => bundles.SetTicketRedeemedAsync(command.Uuid, command.Redeemed);
    }
}
