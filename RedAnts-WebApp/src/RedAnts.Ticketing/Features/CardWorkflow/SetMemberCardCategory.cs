using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class SetMemberCardCategory
{
    public sealed record Command(Guid Uuid, MemberCategory Category);

    public sealed class Handler(IMemberCards cards)
    {
        public async Task HandleAsync(Command command)
        {
            var card = await cards.GetByUuidAsync(command.Uuid) ?? throw new DomainException(EditMemberCard.NotFound);
            card.SetCategory(command.Category);
            await cards.SaveAsync(card);
        }
    }
}
