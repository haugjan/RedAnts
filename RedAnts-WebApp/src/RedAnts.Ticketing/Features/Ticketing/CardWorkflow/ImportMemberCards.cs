using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class ImportMemberCards
{
    public sealed record Command(int SeasonId, string Reference, MemberCategory Category, IReadOnlyList<MemberImportRow> Rows,
        string? CreatedByName, string? CreatedByEmail);

    public sealed class Handler(IMemberCards cards)
    {
        public Task<int> HandleAsync(Command command) =>
            cards.ImportAsync(command.SeasonId, command.Reference, command.Category, command.Rows, command.CreatedByName, command.CreatedByEmail);
    }
}
