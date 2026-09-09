using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.SeasonPasses;

public static class ImportSeasonPasses
{
    public sealed record Command(int SeasonId, IReadOnlyList<TicketImportRow> Rows, string DefaultBundle, int? DefaultTierId,
        string? CreatedByName, string? CreatedByEmail);

    public sealed class Handler(ISeasonPassRepository passes, IUnitOfWork unitOfWork)
    {
        public Task<(int Created, int Updated)> HandleAsync(Command command) =>
            unitOfWork.RunAsync(() => passes.ImportUnifiedAsync(command.SeasonId, command.Rows, command.DefaultBundle, command.DefaultTierId,
                command.CreatedByName, command.CreatedByEmail));
    }
}
