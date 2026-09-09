using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;

namespace RedAnts.Ticketing.Features.MemberCards;

public static class ImportMemberCards
{
    public sealed record Command(int SeasonId, string Reference, MemberCategory Category, IReadOnlyList<MemberImportRow> Rows,
        string? CreatedByName, string? CreatedByEmail);

    public sealed class Handler(IMemberCardRepository cards, IUnitOfWork unitOfWork)
    {
        public Task<int> HandleAsync(Command command) =>
            unitOfWork.RunAsync(() => cards.ImportAsync(command.SeasonId, command.Reference, command.Category, command.Rows,
                command.CreatedByName, command.CreatedByEmail));
    }
}
