using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.MemberCards;

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
