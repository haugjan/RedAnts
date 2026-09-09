using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.MemberCards;

public static class EditMemberCard
{
    public const string NotFound = "Mitgliederkarte wurde nicht gefunden.";

    public sealed record Command(Guid Uuid, string? FirstName, string? LastName, DateOnly? Birthday, MemberCategory Category,
        TicketStatus Status, string? Reference, string? Email, MemberAddress? Address, int Admissions);

    public sealed class Handler(IMemberCards cards)
    {
        public async Task HandleAsync(Command command)
        {
            var card = await cards.GetByUuidAsync(command.Uuid) ?? throw new DomainException(NotFound);
            card.Edit(command.FirstName, command.LastName, command.Birthday, command.Category, command.Status,
                command.Reference, command.Email, command.Address, command.Admissions);
            await cards.SaveAsync(card);
        }
    }
}
