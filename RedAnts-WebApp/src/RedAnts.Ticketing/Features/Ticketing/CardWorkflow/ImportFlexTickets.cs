using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class ImportFlexTickets
{
    public sealed record Command(int SeasonId, IReadOnlyList<TicketImportRow> Rows, string DefaultBundle, TicketCategory DefaultCategory,
        string? CreatedByName, string? CreatedByEmail);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task<(int Created, int Updated)> HandleAsync(Command command) =>
            bundles.ImportUnifiedAsync(command.SeasonId, command.Rows, command.DefaultBundle, command.DefaultCategory,
                command.CreatedByName, command.CreatedByEmail);
    }
}
