using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class SetMemberCardStatus
{
    public sealed record Command(Guid Uuid, TicketStatus Status);

    public sealed class Handler(IMemberCards cards)
    {
        public async Task HandleAsync(Command command)
        {
            var card = await cards.GetByUuidAsync(command.Uuid) ?? throw new DomainException(EditMemberCard.NotFound);
            card.SetStatus(command.Status);
            await cards.SaveAsync(card);
        }
    }
}
