using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public static class PrintTickets
{
    public sealed record Command(
        TicketType Type, byte[] Template, TicketPrintLayout Layout, int? BundleId, int? SeasonId, string? Reference, Guid? Uuid);

    public sealed class Handler(IPrintItemsReader printItems, ITicketPrinter printer, ITicketPrintSettings settings)
    {
        public async Task<byte[]?> HandleAsync(Command command)
        {
            var items = await printItems.ResolveAsync(command.Type, command.BundleId, command.SeasonId, command.Reference, command.Uuid);
            if (items.Count == 0) return null;
            var pdf = await printer.BuildAsync(items, command.Template, command.Layout);
            await settings.SaveAsync(command.Type, command.Layout);
            return pdf;
        }
    }
}
