using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.MemberCards;

public static class CreateMemberCard
{
    public sealed record Command(int SeasonId, MemberCategory Category, string? FirstName, string? LastName, DateOnly? Birthday,
        string Reference, string? Email, MemberAddress? Address, int Admissions, string? CreatedByName, string? CreatedByEmail);

    public sealed class Handler(IMemberCardRepository cards)
    {
        public Task HandleAsync(Command command)
        {
            var reference = (command.Reference ?? "").Trim();
            if (reference.Length == 0) throw new DomainException("Ein Bundle muss angegeben werden.");
            var card = MemberCard.Create(command.SeasonId, command.Category, command.FirstName, command.LastName, command.Birthday,
                email: command.Email, reference: reference, createdByName: command.CreatedByName, createdByEmail: command.CreatedByEmail,
                address: command.Address, admissions: command.Admissions);
            return cards.AddAsync(card);
        }
    }
}
