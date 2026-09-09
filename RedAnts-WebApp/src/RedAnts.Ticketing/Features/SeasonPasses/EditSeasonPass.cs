using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.SeasonPasses;

public static class EditSeasonPass
{
    public const string NotFound = "Saisonkarte wurde nicht gefunden.";

    public sealed record Command(Guid Uuid, decimal Price, TicketStatus Status, int? TierId, Buyer? Buyer, string? Email);

    public sealed class Handler(ISeasonPasses passes)
    {
        public async Task HandleAsync(Command command)
        {
            var pass = await passes.GetByUuidAsync(command.Uuid) ?? throw new DomainException(NotFound);
            pass.Edit(command.Price, command.Status, command.TierId);
            if (command.Buyer is { } buyer) pass.SetBuyer(buyer);
            if (command.Email is not null) pass.SetEmail(command.Email);
            await passes.SaveAsync(pass);
        }
    }
}
