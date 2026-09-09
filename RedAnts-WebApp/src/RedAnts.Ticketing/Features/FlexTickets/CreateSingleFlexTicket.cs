using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.FlexTickets;

public static class CreateSingleFlexTicket
{
    public sealed record Command(int SeasonId, TicketCategory Category, string Reference, CardHolder Holder, string? CreatedByName, string? CreatedByEmail);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task<Guid> HandleAsync(Command command)
        {
            var reference = FlexBundleReference.Clean(command.Reference);
            if (!command.Holder.HasName) throw new DomainException("Bitte Vor-/Nachname oder Firmenname angeben.");
            return bundles.CreateSingleAsync(command.SeasonId, command.Category, reference, command.Holder, command.CreatedByName, command.CreatedByEmail);
        }
    }
}
