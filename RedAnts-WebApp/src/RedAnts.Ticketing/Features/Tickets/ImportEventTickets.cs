using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Features.EventBundles;

namespace RedAnts.Ticketing.Features.Tickets;

public static class ImportEventTickets
{
    public sealed record Command(int EventId, IReadOnlyList<TicketImportRow> Rows, string DefaultBundle, TicketCategory DefaultCategory,
        string? CreatedByName, string? CreatedByEmail);

    public sealed class Handler(IEventTicketBundleRepository bundles, IUnitOfWork unitOfWork)
    {
        public Task<(int Created, int Updated)> HandleAsync(Command command) =>
            unitOfWork.RunAsync(() => bundles.ImportUnifiedAsync(command.EventId, command.Rows, command.DefaultBundle, command.DefaultCategory,
                command.CreatedByName, command.CreatedByEmail));
    }
}
