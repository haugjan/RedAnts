using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class CreateMemberCard
{
    public sealed record Command(int SeasonId, MemberCategory Category, string? FirstName, string? LastName, DateOnly? Birthday,
        string Reference, string? Email, MemberAddress? Address, int Admissions, string? CreatedByName, string? CreatedByEmail);

    public sealed class Handler(IMemberCards cards)
    {
        public Task HandleAsync(Command command) =>
            cards.CreateAsync(command.SeasonId, command.Category, command.FirstName, command.LastName, command.Birthday,
                command.Reference, command.Email, command.CreatedByName, command.CreatedByEmail, command.Address, command.Admissions);
    }
}
